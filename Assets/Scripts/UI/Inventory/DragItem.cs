using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItem
: MonoBehaviour, IDragHandler, IPointerDownHandler, IEndDragHandler
{
    private Vector2 pointerOffset;

    private RectTransform rectTransform;

    private RectTransform rectTransformSlot;

    private CanvasGroup canvasGroup;

    private GameObject oldSlot;

    private Inventory inventory;

    private GameObject player;

    private Transform draggedItemBox;

    public delegate void ItemDelegate();

    public static event ItemDelegate updateInventoryList;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransformSlot =
            GameObject
                .FindGameObjectWithTag("DraggingItem")
                .GetComponent<RectTransform>();
        inventory = transform.parent.parent.parent.GetComponent<Inventory>();
        player = GameObject.FindGameObjectWithTag("Player");
        draggedItemBox =
            GameObject.FindGameObjectWithTag("DraggingItem").transform;
    }

    public void OnDrag(PointerEventData data)
    {
        if (rectTransform == null) return;

        if (data.button == PointerEventData.InputButton.Left)
        {
            rectTransform.SetAsLastSibling();
            transform.SetParent (draggedItemBox);
            Vector2 localPointerPosition;
            canvasGroup.blocksRaycasts = false;
            if (
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(rectTransformSlot,
                    Input.mousePosition,
                    data.pressEventCamera,
                    out localPointerPosition)
            )
            {
                rectTransform.localPosition =
                    localPointerPosition - pointerOffset;
            }
        }

        inventory.OnUpdateItemList();
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
        {
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(rectTransform,
                data.position,
                data.pressEventCamera,
                out pointerOffset);
            oldSlot = transform.parent.gameObject;
        }
        if (updateInventoryList != null) updateInventoryList();
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
        {
            canvasGroup.blocksRaycasts = true;
            Transform newSlot = null;
            if (data.pointerEnter != null)
                newSlot = data.pointerEnter.transform;

            if (newSlot != null)
            {
                //getting the items from the slots, GameObjects and RectTransform
                GameObject firstItemGameObject = this.gameObject;
                GameObject secondItemGameObject = newSlot.parent.gameObject;
                RectTransform firstItemRectTransform =
                    this.gameObject.GetComponent<RectTransform>();
                RectTransform secondItemRectTransform =
                    newSlot.parent.GetComponent<RectTransform>();
                Item firstItem =
                    rectTransform.GetComponent<ItemOnObject>().item;
                Item secondItem = new Item();
                if (newSlot.parent.GetComponent<ItemOnObject>() != null)
                    secondItem =
                        newSlot.parent.GetComponent<ItemOnObject>().item;

                //get some informations about the two items
                bool sameItem = firstItem.itemName == secondItem.itemName;
                bool sameItemRerferenced = firstItem.Equals(secondItem);
                bool secondItemStack = false;
                bool firstItemStack = false;
                if (sameItem)
                {
                    firstItemStack = firstItem.itemStack < firstItem.maxStack;
                    secondItemStack =
                        secondItem.itemStack < secondItem.maxStack;
                }

                GameObject Inventory =
                    secondItemRectTransform.parent.gameObject;
                if (Inventory.tag == "Slot")
                    Inventory =
                        secondItemRectTransform.parent.parent.parent.gameObject;

                if (Inventory.tag.Equals("Slot"))
                    Inventory = Inventory.transform.parent.parent.gameObject;

                //dragging in an Inventory
                if (Inventory.tag == "MainInventory")
                {
                    int newSlotChildCount = newSlot.transform.parent.childCount;
                    bool isOnSlot =
                        newSlot.transform.parent.GetChild(0).tag == "ItemIcon";

                    //dragging on a slot where allready is an item on
                    if (newSlotChildCount != 0 && isOnSlot)
                    {
                        //check if the items fits into the other item
                        bool fitsIntoStack = false;
                        if (sameItem)
                            fitsIntoStack =
                                (firstItem.itemStack + secondItem.itemStack) <=
                                firstItem.maxStack;

                        //if the item is stackable checking if the firstitemstack and seconditemstack is not full and check if they are the same items
                        if (
                            inventory.stackable &&
                            sameItem &&
                            firstItemStack &&
                            secondItemStack
                        )
                        {
                            //if the item does not fit into the other item
                            if (fitsIntoStack && !sameItemRerferenced)
                            {
                                secondItem.itemStack =
                                    firstItem.itemStack + secondItem.itemStack;
                                secondItemGameObject
                                    .transform
                                    .SetParent(newSlot.parent.parent);
                                Destroy (firstItemGameObject);
                                secondItemRectTransform.localPosition =
                                    Vector3.zero;
                            }
                            else
                            {
                                //creates the rest of the item
                                int rest =
                                    (
                                    firstItem.itemStack + secondItem.itemStack
                                    ) %
                                    firstItem.maxStack;

                                //fill up the other stack and adds the rest to the other stack
                                if (!fitsIntoStack && rest > 0)
                                {
                                    firstItem.itemStack = firstItem.maxStack;
                                    secondItem.itemStack = rest;

                                    firstItemGameObject
                                        .transform
                                        .SetParent(secondItemGameObject
                                            .transform
                                            .parent);
                                    secondItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);

                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                    secondItemRectTransform.localPosition =
                                        Vector3.zero;
                                }
                            }
                        }
                        else
                        //if does not fit
                        {
                            //creates the rest of the item
                            int rest = 0;
                            if (sameItem)
                                rest =
                                    (
                                    firstItem.itemStack + secondItem.itemStack
                                    ) %
                                    firstItem.maxStack;

                            //fill up the other stack and adds the rest to the other stack
                            if (!fitsIntoStack && rest > 0)
                            {
                                secondItem.itemStack = firstItem.maxStack;
                                firstItem.itemStack = rest;

                                firstItemGameObject
                                    .transform
                                    .SetParent(secondItemGameObject
                                        .transform
                                        .parent);
                                secondItemGameObject
                                    .transform
                                    .SetParent(oldSlot.transform);

                                firstItemRectTransform.localPosition =
                                    Vector3.zero;
                                secondItemRectTransform.localPosition =
                                    Vector3.zero;
                            } //if they are different items or the stack is full, they get swapped
                            else if (!fitsIntoStack && rest == 0)
                            {
                                //if you are dragging an item from equipmentsystem to the inventory and try to swap it with the same itemtype
                                if (
                                    oldSlot
                                        .transform
                                        .parent
                                        .parent
                                        .tag ==
                                    "EquipmentSystem" &&
                                    firstItem.itemType == secondItem.itemType
                                )
                                {
                                    newSlot
                                        .transform
                                        .parent
                                        .parent
                                        .parent
                                        .parent
                                        .GetComponent<Inventory>()
                                        .UnEquipItem1(firstItem);
                                    oldSlot
                                        .transform
                                        .parent
                                        .parent
                                        .GetComponent<Inventory>()
                                        .EquipItem(secondItem);

                                    firstItemGameObject
                                        .transform
                                        .SetParent(secondItemGameObject
                                            .transform
                                            .parent);
                                    secondItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);
                                    secondItemRectTransform.localPosition =
                                        Vector3.zero;
                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                } //if you are dragging an item from the equipmentsystem to the inventory and they are not from the same itemtype they do not get swapped.
                                else if (
                                    oldSlot
                                        .transform
                                        .parent
                                        .parent
                                        .tag == "EquipmentSystem" &&
                                    firstItem.itemType != secondItem.itemType
                                )
                                {
                                    firstItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);
                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                } //swapping for the rest of the inventorys
                                else if (
                                    oldSlot
                                        .transform
                                        .parent
                                        .parent
                                        .tag != "EquipmentSystem"
                                )
                                {
                                    firstItemGameObject
                                        .transform
                                        .SetParent(secondItemGameObject
                                            .transform
                                            .parent);
                                    secondItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);
                                    secondItemRectTransform.localPosition =
                                        Vector3.zero;
                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                }
                            }
                        }
                    }
                    else
                    //empty slot
                    {
                        if (newSlot.tag != "Slot" && newSlot.tag != "ItemIcon")
                        {
                            firstItemGameObject
                                .transform
                                .SetParent(oldSlot.transform);
                            firstItemRectTransform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            firstItemGameObject
                                .transform
                                .SetParent(newSlot.transform);
                            firstItemRectTransform.localPosition = Vector3.zero;

                            if (
                                newSlot
                                    .transform
                                    .parent
                                    .parent
                                    .tag != "EquipmentSystem" &&
                                oldSlot
                                    .transform
                                    .parent
                                    .parent
                                    .tag == "EquipmentSystem"
                            )
                                oldSlot
                                    .transform
                                    .parent
                                    .parent
                                    .GetComponent<Inventory>()
                                    .UnEquipItem1(firstItem);
                        }
                    }
                } //dragging into a Hotbar
                else if (Inventory.tag == "Hotbar")
                {
                    int newSlotChildCount = newSlot.transform.parent.childCount;
                    bool isOnSlot =
                        newSlot.transform.parent.GetChild(0).tag == "ItemIcon";
                    int siblingIndex = newSlot.transform.GetSiblingIndex();

                    //dragging on a slot where allready is an item on
                    if (newSlotChildCount != 0 && isOnSlot)
                    {
                        //check if the items fits into the other item
                        bool fitsIntoStack = false;
                        if (sameItem)
                            fitsIntoStack =
                                (firstItem.itemStack + secondItem.itemStack) <=
                                firstItem.maxStack;

                        //if the item is stackable checking if the firstitemstack and seconditemstack is not full and check if they are the same items
                        if (
                            inventory.stackable &&
                            sameItem &&
                            firstItemStack &&
                            secondItemStack
                        )
                        {
                            //if the item does not fit into the other item
                            if (fitsIntoStack && !sameItemRerferenced)
                            {
                                secondItem.itemStack =
                                    firstItem.itemStack + secondItem.itemStack;
                                secondItemGameObject
                                    .transform
                                    .SetParent(newSlot.parent.parent);
                                Destroy (firstItemGameObject);
                                secondItemRectTransform.localPosition =
                                    Vector3.zero;
                            }
                            else
                            {
                                //creates the rest of the item
                                int rest =
                                    (
                                    firstItem.itemStack + secondItem.itemStack
                                    ) %
                                    firstItem.maxStack;

                                //fill up the other stack and adds the rest to the other stack
                                if (!fitsIntoStack && rest > 0)
                                {
                                    firstItem.itemStack = firstItem.maxStack;
                                    secondItem.itemStack = rest;

                                    firstItemGameObject
                                        .transform
                                        .SetParent(secondItemGameObject
                                            .transform
                                            .parent);
                                    secondItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);

                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                    secondItemRectTransform.localPosition =
                                        Vector3.zero;
                                }
                            }
                        }
                        else
                        //if does not fit
                        {
                            //creates the rest of the item
                            int rest = 0;
                            if (sameItem)
                                rest =
                                    (
                                    firstItem.itemStack + secondItem.itemStack
                                    ) %
                                    firstItem.maxStack;

                            bool fromEquip =
                                oldSlot
                                    .transform
                                    .parent
                                    .parent
                                    .tag == "EquipmentSystem";

                            //fill up the other stack and adds the rest to the other stack
                            if (!fitsIntoStack && rest > 0)
                            {
                                secondItem.itemStack = firstItem.maxStack;
                                firstItem.itemStack = rest;

                                firstItemGameObject
                                    .transform
                                    .SetParent(secondItemGameObject
                                        .transform
                                        .parent);
                                secondItemGameObject
                                    .transform
                                    .SetParent(oldSlot.transform);

                                firstItemRectTransform.localPosition =
                                    Vector3.zero;
                                secondItemRectTransform.localPosition =
                                    Vector3.zero;
                            } //if they are different items or the stack is full, they get swapped
                            else if (!fitsIntoStack && rest == 0)
                            {
                                if (!fromEquip)
                                {
                                    firstItemGameObject
                                        .transform
                                        .SetParent(secondItemGameObject
                                            .transform
                                            .parent);
                                    secondItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);
                                    secondItemRectTransform.localPosition =
                                        Vector3.zero;
                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;

                                }
                                else
                                {
                                    firstItemGameObject
                                        .transform
                                        .SetParent(oldSlot.transform);
                                    firstItemRectTransform.localPosition =
                                        Vector3.zero;
                                }
                            }
                        }
                    }
                    else
                    //empty slot
                    {
                        if (newSlot.tag != "Slot" && newSlot.tag != "ItemIcon")
                        {
                            firstItemGameObject
                                .transform
                                .SetParent(oldSlot.transform);
                            firstItemRectTransform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            firstItemGameObject
                                .transform
                                .SetParent(newSlot.transform);
                            firstItemRectTransform.localPosition = Vector3.zero;

                            if (
                                newSlot
                                    .transform
                                    .parent
                                    .parent
                                    .tag !=
                                "EquipmentSystem" &&
                                oldSlot
                                    .transform
                                    .parent
                                    .parent
                                    .tag == "EquipmentSystem"
                            )
                                oldSlot
                                    .transform
                                    .parent
                                    .parent
                                    .GetComponent<Inventory>()
                                    .UnEquipItem1(firstItem);
                        }
                    }
                } //dragging into a equipmentsystem/charactersystem
                else if (Inventory.tag == "EquipmentSystem")
                {
                    ItemType[] itemTypeOfSlots =
                        {ItemType.Head, ItemType.Chest, ItemType.Trouser, ItemType.Shoe};
                    int newSlotChildCount = newSlot.transform.parent.childCount;
                    bool isOnSlot =
                        newSlot.transform.parent.GetChild(0).tag == "ItemIcon";
                    bool sameItemType =
                        firstItem.itemType == secondItem.itemType;
                    bool fromHot =
                        oldSlot
                            .transform
                            .parent
                            .parent
                            .tag ==
                        "Hotbar";

                    //dragging on a slot where allready is an item on
                    if (newSlotChildCount != 0 && isOnSlot)
                    {
                        //items getting swapped if they are the same itemtype
                        if (
                            sameItemType && !sameItemRerferenced //
                        )
                        {
                            Transform temp1 =
                                secondItemGameObject
                                    .transform
                                    .parent
                                    .parent
                                    .parent;
                            Transform temp2 = oldSlot.transform.parent.parent;

                            firstItemGameObject
                                .transform
                                .SetParent(secondItemGameObject
                                    .transform
                                    .parent);
                            secondItemGameObject
                                .transform
                                .SetParent(oldSlot.transform);
                            secondItemRectTransform.localPosition =
                                Vector3.zero;
                            firstItemRectTransform.localPosition = Vector3.zero;

                            if (!temp1.Equals(temp2))
                            {
                                if (firstItem.itemType == ItemType.UFPS_Weapon)
                                {
                                    Inventory
                                        .GetComponent<Inventory>()
                                        .UnEquipItem1(secondItem);
                                    Inventory
                                        .GetComponent<Inventory>()
                                        .EquipItem(firstItem);
                                }
                                else
                                {
                                    Inventory
                                        .GetComponent<Inventory>()
                                        .EquipItem(firstItem);
                                    if (secondItem.itemType != ItemType.Backpack
                                    )
                                        Inventory
                                            .GetComponent<Inventory>()
                                            .UnEquipItem1(secondItem);
                                }
                            }

                        }
                        else
                        //if they are not from the same Itemtype the dragged one getting placed back
                        {
                            firstItemGameObject
                                .transform
                                .SetParent(oldSlot.transform);
                            firstItemRectTransform.localPosition = Vector3.zero;
                        }
                    }
                    else
                    //if the slot is empty
                    {
                        for (int i = 0; i < newSlot.parent.childCount; i++)
                        {
                            if (newSlot.Equals(newSlot.parent.GetChild(i)))
                            {
                                //checking if it is the right slot for the item
                                if (
                                    itemTypeOfSlots[i] ==
                                    transform
                                        .GetComponent<ItemOnObject>()
                                        .item
                                        .itemType
                                )
                                {
                                    transform.SetParent (newSlot);
                                    rectTransform.localPosition = Vector3.zero;

                                    if (
                                        !oldSlot
                                            .transform
                                            .parent
                                            .parent
                                            .Equals(newSlot
                                                .transform
                                                .parent
                                                .parent)
                                    )
                                        Inventory
                                            .GetComponent<Inventory>()
                                            .EquipItem(firstItem);
                                }
                                else
                                //else it get back to the old slot
                                {
                                    transform.SetParent(oldSlot.transform);
                                    rectTransform.localPosition = Vector3.zero;
                                }
                            }
                        }
                    }
                }
                else
                {
                    DropItem();
                }
            }
            else
            {
                DropItem(); 
            }
        }
        inventory.OnUpdateItemList();
    }

    private void DropItem()
    {
        GameObject dropItem =
            (GameObject)
            Instantiate(GameObject.FindGameObjectWithTag("GameController").GetComponent<ItemDataBaseList>().pickupItem);
        dropItem.AddComponent<PickUpItem>();
        dropItem.GetComponent<PickUpItem>().item =
            this.gameObject.GetComponent<ItemOnObject>().item;

        dropItem.transform.localPosition =
            player.transform.localPosition +
            player.transform.forward * 3 +
            new Vector3(0, .5f, 0);
        inventory.OnUpdateItemList();
        if (
            oldSlot.transform.parent.parent.tag == "EquipmentSystem"
        )
            inventory
                .GetComponent<Inventory>()
                .UnEquipItem1(dropItem.GetComponent<PickUpItem>().item);
        Destroy(this.gameObject);
    }
}
