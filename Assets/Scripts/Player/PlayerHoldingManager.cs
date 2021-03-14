using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof (PlayerInputHandler))]
public class PlayerHoldingManager : MonoBehaviour
{
    public enum SwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew
    }

    [Header("References")]
    public Camera itemCamera;

    public Transform itemParentSocket;

    public Transform defaultItemPosition;

    public Transform downItemPosition;

    [Header("Bobbing")]
    public float bobFrequency = 10f;

    public float bobSharpness = 10f;

    public float bobAmount = 0.05f;

    [Header("Misc")]
    public float damageMultiplier = 1f;

    public float itemSwitchDelay = 1f;

    [Tooltip("Layer to set item gameObjects to")]
    public LayerMask FPSItemLayer;

    public bool isPointingAtEnemy { get; private set; }

    public int activeItemIndex { get; private set; }

    public UnityAction<ItemController> onSwitchedToItem;

    public UnityAction<ItemController, int> onAddedItem;

    public UnityAction<ItemController, int> onRemovedItem;

    ItemController[] m_Hotbar = new ItemController[9];

    public PlayerInputHandler m_InputHandler;

    public PlayerCharacterController m_PlayerCharacterController;

    public PlayerInventory m_Inventory;

    float m_BobFactor;

    Vector3 m_LastCharacterPosition;

    Vector3 m_MainLocalPosition;

    Vector3 m_BobLocalPosition;

    float m_TimeStartedItemSwitch;

    SwitchState m_ItemSwitchState;

    int m_ItemSwitchNewItemIndex;

    private void Start()
    {
        activeItemIndex = 0;
        m_ItemSwitchState = SwitchState.Down;

        onSwitchedToItem += OnItemSwitched;

        SwitchItem(activeItemIndex);
    }

    private void Update()
    {
        // handling
        ItemController activeItem = GetActiveItem();

        if (activeItem && m_ItemSwitchState == SwitchState.Up)
        {
            activeItem
                .HandleUseInputs(m_InputHandler.GetUseInputDown(),
                m_InputHandler.GetUseInputHeld(),
                m_InputHandler.GetUseInputReleased());
        }

        // item switch handling
        if (
            (activeItem == null || !activeItem.isCharging) &&
            (
            m_ItemSwitchState == SwitchState.Up ||
            m_ItemSwitchState == SwitchState.Down
            )
        )
        {
            int switchItemInput = m_InputHandler.GetSwitchItemInput();
            if (switchItemInput != 0)
            {
                SwitchItem (switchItemInput);
            }
            else
            {
                switchItemInput = m_InputHandler.GetSelectItemInput();
                if (switchItemInput != 0)
                {
                    SwitchToItemIndex(switchItemInput - 1);
                }
            }
        }

        // Pointing at enemy handling
        isPointingAtEnemy = false;
        if (activeItem)
        {
            if (
                Physics
                    .Raycast(itemCamera.transform.position,
                    itemCamera.transform.forward,
                    out RaycastHit hit,
                    1000,
                    -1,
                    QueryTriggerInteraction.Ignore)
            )
            {
                if (hit.collider.GetComponentInParent<EnemyController>())
                {
                    isPointingAtEnemy = true;
                }
            }
        }
    }

    // Update various animated features in LateUpdate because it needs to override the animated arm position
    private void LateUpdate()
    {
        UpdateBob();
        UpdateItemSwitching();

        // Set final item socket position based on all the combined animation influences
        itemParentSocket.localPosition =
            m_MainLocalPosition + m_BobLocalPosition;
    }

    public void SwitchItem(int scroll)
    {
        int newItemIndex = activeItemIndex;
        if (scroll < 0)
        {
            newItemIndex--;
        }
        else
        {
            newItemIndex++;
        }

        newItemIndex = (newItemIndex + 9) % 9;

        // Handle switching to the new index
        SwitchToItem (newItemIndex);
    }

    public void SwitchToItem(int newItemIndex)
    {
        if (newItemIndex != activeItemIndex && newItemIndex >= 0)
        {
            // Store data related to weapon switching animation
            m_ItemSwitchNewItemIndex = newItemIndex;
            m_TimeStartedWItemwitch = Time.time;

            if (GetActiveItem() == null)
            {
                m_MainLocalPosition = downItemPosition.localPosition;
                m_ItemSwitchState = SwitchState.PutUpNew;
                activeItemIndex = m_ItemSwitchNewItemIndex;

                ItemController newItem = Item(m_ItemSwitchNewItemIndex);
                if (onSwitchedToItem != null)
                {
                    onSwitchedToItem.Invoke (newItem);
                }
            }
            else
            {
                m_ItemSwitchState = SwitchState.PutDownPrevious;
            }
        }
    }

    // Updates the bob animation based on character speed
    void UpdateBob()
    {
        if (Time.deltaTime > 0f)
        {
            Vector3 playerCharacterVelocity =
                (
                m_PlayerCharacterController.transform.position -
                m_LastCharacterPosition
                ) /
                Time.deltaTime;

            // calculate a smoothed weapon bob amount based on how close to our max grounded movement velocity we are
            float characterMovementFactor = 0f;
            if (m_PlayerCharacterController.isGrounded)
            {
                characterMovementFactor =
                    Mathf
                        .Clamp01(playerCharacterVelocity.magnitude /
                        (
                        m_PlayerCharacterController.maxSpeedOnGround *
                        m_PlayerCharacterController.sprintSpeedModifier
                        ));
            }
            m_BobFactor =
                Mathf
                    .Lerp(m_BobFactor,
                    characterMovementFactor,
                    bobSharpness * Time.deltaTime);

            // Calculate vertical and horizontal bob values based on a sine function
            float frequency = bobFrequency;
            float hBobValue =
                Mathf.Sin(Time.time * frequency) * bobAmount * m_BobFactor;
            float vBobValue =
                ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) *
                bobAmount *
                m_BobFactor;

            // Apply weapon bob
            m_WeaponBobLocalPosition.x = hBobValue;
            m_WeaponBobLocalPosition.y = Mathf.Abs(vBobValue);

            m_LastCharacterPosition =
                m_PlayerCharacterController.transform.position;
        }
    }

    // Updates the animated transition of switching

    void UpdateItemSwitching()
    {
        // Calculate the time ratio (0 to 1) since switch was triggered
        float switchingTimeFactor = 0f;
        if (itemSwitchDelay == 0f)
        {
            switchingTimeFactor = 1f;
        }
        else
        {
            switchingTimeFactor =
                Mathf
                    .Clamp01((Time.time - m_TimeStartedItemSwitch) /
                    itemSwitchDelay);
        }

        // Handle transiting to new switch state
        if (switchingTimeFactor >= 1f)
        {
            if (m_ItemSwitchState == SwitchState.PutDownPrevious)
            {
                // Deactivate old

                ItemController old = GetActiveItem(activeItemIndex);
                if (old != null)
                {
                    old.Show(false);
                }

                activeItemIndex = m_ItemSwitchNewItemIndex;
                switchingTimeFactor = 0f;

                // Activate new weapon
                ItemController newItem = GetActiveItem(activeItemIndex);
                if (onSwitchedToItem != null)
                {
                    onSwitchedToItem.Invoke (newItem);
                }

                if (newItem)
                {
                    m_TimeStartedWItemwitch = Time.time;
                    m_ItemSwitchState = SwitchState.PutUpNew;
                }
                else
                {
                    // if new item is null, don't follow through with putting item back up
                    m_ItemSwitchState = SwitchState.Down;
                }
            }
            else if (m_ItemSwitchState == SwitchState.PutUpNew)
            {
                m_ItemSwitchState = SwitchState.Up;
            }
        }

        // Handle moving the socket position for the animated switching
        if (m_ItemSwitchState == SwitchState.PutDownPrevious)
        {
            m_MainLocalPosition =
                Vector3
                    .Lerp(defaultItemPosition.localPosition,
                    downItemPosition.localPosition,
                    switchingTimeFactor);
        }
        else if (m_ItemSwitchState == SwitchState.PutUpNew)
        {
            m_MainLocalPosition =
                Vector3
                    .Lerp(downItemPosition.localPosition,
                    defaultItemPosition.localPosition,
                    switchingTimeFactor);
        }
    }

    // Called when an item is moved to the hotbar
    public void AddItem(ItemController itemPrefab, int index)
    {
        // spawn the prefab as child of the item socket
        ItemController instance = Instantiate(itemPrefab, itemParentSocket);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        // Set owner to this gameObject so the item can alter logic accordingly
        instance.owner = gameObject;
        instance.sourcePrefab = itemPrefab.gameObject;
        instance.Show(false);

        // Assign the first person layer to the item
        int layerIndex = Mathf.RoundToInt(Mathf.Log(FPSItemLayer.value, 2)); // This function converts a layermask to a layer index
        foreach (Transform
            t
            in
            instance.gameObject.GetComponentsInChildren<Transform>(true)
        )
        {
            t.gameObject.layer = layerIndex;
        }

        m_Hotbar[index] = instance;

        if (onAddedItem != null)
        {
            onAddedItem.Invoke (instance, i);
        }
    }

    //called when item is taken out of hotbar
    public void RemoveItem(int index)
    {
        m_Hotbar[i] = null;

        if (onRemovedItem != null)
        {
            onRemovedItem.Invoke (instance, i);
        }

        Destroy(instance.gameObject);
    }

    public ItemController GetActiveItem()
    {
        return m_Hotbar[activeItemIndex];
    }

    void OnItemSwitched(ItemController newItem)
    {
        if (newItem != null)
        {
            newItem.Show(true);
        }
    }
}
