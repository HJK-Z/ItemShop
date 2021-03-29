using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof (PlayerInputHandler))]
public class HotbarManager : MonoBehaviour
{
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
    public float damageMultiplier = 1f; //relocate later

    public float itemSwitchDelay = 1f;

    [Tooltip("Layer to set item gameObjects to")]
    public LayerMask FPSItemLayer;

    public bool isPointingAtEnemy { get; private set; }

    public int activeItemIndex { get; private set; }

    public GameObject activeSlot;

    public GameObject itemPrefab;

    public UnityAction<ItemController> onSwitchedToItem;

    public UnityAction<ItemController, int> onAddedItem;

    public UnityAction<ItemController, int> onRemovedItem;

    public ItemController[] m_Hotbar = new ItemController[9];

    public PlayerInputHandler m_InputHandler;

    public PlayerCharacterController m_PlayerCharacterController;

    public PlayerInventory m_Inventory;

    float m_BobFactor;

    Vector3 m_LastCharacterPosition;

    Vector3 m_MainLocalPosition;

    Vector3 m_BobLocalPosition;

    private void Start()
    {
        activeItemIndex = 0;

        onSwitchedToItem += OnItemSwitched;

        for (int i = 0; i < 9; i++)
        {
            AddItem(i, new Item());
        }

        SwitchToItem (activeItemIndex);
    }

    private void Update()
    {
        // handling
        ItemController activeItem = GetActiveItem();

        if (activeItem)
        {
            activeItem
                .HandleUseInputs(m_InputHandler.GetUseInputDown(),
                m_InputHandler.GetUseInputHeld(),
                m_InputHandler.GetUseInputReleased());
        }

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
                SwitchToItem(switchItemInput - 1);
            }
        }

        // Update items in hotbar
        for (int i = 0; i < 9; i++)
        {
            Transform itemSlot =
                GameObject
                    .FindGameObjectsWithTag("Hotbar")[0]
                    .transform
                    .GetChild(0)
                    .GetChild(i);
            if (
                itemSlot.childCount == 0 ||
                (
                itemSlot.childCount == 1 &&
                itemSlot.GetChild(0).name == "ActiveSlot"
                )
            )
            {
                if (m_Hotbar[i].item.itemID != -1)
                {
                    m_Hotbar[i].UpdateItem(new Item());
                }
            }
            else if (itemSlot.GetChild(0).name == "ActiveSlot")
            {
                Item it =
                    itemSlot.GetChild(1).GetComponent<ItemOnObject>().item;
                if (m_Hotbar[i].item.itemID != it.itemID)
                {
                    m_Hotbar[i].UpdateItem(it);
                    if (!m_Hotbar[i].isWeaponActive)
                    {
                        m_Hotbar[i].Show(true);
                    }
                }
            }
            else
            {
                Item it =
                    itemSlot.GetChild(0).GetComponent<ItemOnObject>().item;
                if (m_Hotbar[i].item.itemID != it.itemID)
                {
                    m_Hotbar[i].UpdateItem(it);
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
            m_Hotbar[activeItemIndex].Show(false);
            activeItemIndex = newItemIndex;

            activeSlot
                .transform
                .SetParent(m_Inventory
                    .hotbar
                    .transform
                    .GetChild(0)
                    .GetChild(activeItemIndex));

            m_Hotbar[activeItemIndex].Show(true);
            activeSlot.transform.localPosition = Vector3.zero;
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
            m_BobLocalPosition.x = hBobValue;
            m_BobLocalPosition.y = Mathf.Abs(vBobValue);

            m_LastCharacterPosition =
                m_PlayerCharacterController.transform.position;
        }
    }

    // Called when an item is moved to the hotbar
    public void AddItem(int index, Item item)
    {
        // spawn the prefab as child of the item socket
        ItemController instance =
            Instantiate(itemPrefab, itemParentSocket)
                .GetComponent<ItemController>();
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        // Set owner to this gameObject so the item can alter logic accordingly
        instance.owner = gameObject;
        instance.sourcePrefab = itemPrefab.gameObject;
        instance.item = item;
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
            onAddedItem.Invoke (instance, index);
        }
    }

    // called when item is taken out of hotbar
    public void RemoveItem(int index)
    {
        if (onRemovedItem != null)
        {
            onRemovedItem.Invoke(m_Hotbar[index], index);
        }

        Destroy(m_Hotbar[index].gameObject);
        m_Hotbar[index] = null;
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
