using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    //Prefabs
    [SerializeField]
    private GameObject prefabCanvasWithPanel;

    [SerializeField]
    private GameObject prefabSlot;

    [SerializeField]
    private GameObject prefabSlotContainer;

    [SerializeField]
    private GameObject prefabItem;

    [SerializeField]
    private GameObject prefabDraggingItemContainer;

    [SerializeField]
    private GameObject prefabPanel;

    //Itemdatabase
    [SerializeField]
    private ItemDataBaseList itemDatabase;

    //GameObjects which are alive
    [SerializeField]
    private string inventoryTitle;

    [SerializeField]
    private RectTransform PanelRectTransform;

    [SerializeField]
    private Image PanelImage;

    [SerializeField]
    private GameObject SlotContainer;

    [SerializeField]
    private GameObject DraggingItemContainer;

    [SerializeField]
    private RectTransform SlotContainerRectTransform;

    [SerializeField]
    private GridLayoutGroup SlotGridLayout;

    [SerializeField]
    private RectTransform SlotGridRectTransform;

    //Inventory Settings
    [SerializeField]
    public bool mainInventory;

    [SerializeField]
    public List<Item> ItemsInInventory = new List<Item>();

    [SerializeField]
    public int height;

    [SerializeField]
    public int width;

    [SerializeField]
    public bool stackable;

    [SerializeField]
    public static bool inventoryOpen;

    // GUI Settings
    [SerializeField]
    public int positionNumberX;

    [SerializeField]
    public int positionNumberY;

    //event delegates for consuming, gearing
    public delegate void ItemDelegate(Item item);

    public static event ItemDelegate ItemConsumed;

    public static event ItemDelegate ItemEquip;

    public static event ItemDelegate UnEquipItem;

    public delegate void InventoryOpened();

    public static event InventoryOpened InventoryOpen;

    public static event InventoryOpened AllInventoriesClosed;

    void Start()
    {
        if (transform.GetComponent<Hotbar>() == null)
            this.gameObject.SetActive(false);

        updateItemList();
    }

    public void sortItems()
    {
        int empty = -1;
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (
                SlotContainer.transform.GetChild(i).childCount == 0 &&
                empty == -1
            )
                empty = i;
            else
            {
                if (empty > -1)
                {
                    if (SlotContainer.transform.GetChild(i).childCount != 0)
                    {
                        RectTransform rect =
                            SlotContainer
                                .transform
                                .GetChild(i)
                                .GetChild(0)
                                .GetComponent<RectTransform>();
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .transform
                            .SetParent(SlotContainer
                                .transform
                                .GetChild(empty)
                                .transform);
                        rect.localPosition = Vector3.zero;
                        i = empty + 1;
                        empty = i;
                    }
                }
            }
        }
    }

    void Update()
    {
        updateItemIndex();
    }

    public void setAsMain()
    {
        if (mainInventory)
            this.gameObject.tag = "Untagged";
        else if (!mainInventory) this.gameObject.tag = "MainInventory";
    }

    public void OnUpdateItemList()
    {
        updateItemList();
    }

    public void closeInventory()
    {
        this.gameObject.SetActive(false);
        checkIfAllInventoryClosed();
    }

    public void openInventory()
    {
        this.gameObject.SetActive(true);
        if (InventoryOpen != null) InventoryOpen();
    }

    public void checkIfAllInventoryClosed()
    {
        GameObject canvas = GameObject.FindGameObjectWithTag("Canvas");

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            GameObject child = canvas.transform.GetChild(i).gameObject;
            if (
                !child.activeSelf &&
                (
                child.tag == "EquipmentSystem" ||
                child.tag == "Panel" ||
                child.tag == "MainInventory"
                )
            )
            {
                if (
                    AllInventoriesClosed != null &&
                    i == canvas.transform.childCount - 1
                ) AllInventoriesClosed();
            }
            else if (
                child.activeSelf &&
                (
                child.tag == "EquipmentSystem" ||
                child.tag == "Panel" ||
                child.tag == "MainInventory"
                )
            )
                break;
            else if (i == canvas.transform.childCount - 1)
            {
                if (AllInventoriesClosed != null) AllInventoriesClosed();
            }
        }
    }

    public void ConsumeItem(Item item)
    {
        if (ItemConsumed != null) ItemConsumed(item);
    }

    public void EquiptItem(Item item)
    {
        if (ItemEquip != null) ItemEquip(item);
    }

    public void UnEquipItem1(Item item)
    {
        if (UnEquipItem != null) UnEquipItem(item);
    }

    public void updateItemList()
    {
        ItemsInInventory.Clear();
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            Transform trans = SlotContainer.transform.GetChild(i);
            if (trans.childCount != 0 && trans.GetChild(0).name != "ActiveSlot")
            {
                ItemsInInventory
                    .Add(trans.GetChild(0).GetComponent<ItemOnObject>().item);
            }
        }
    }

    public bool characterSystem()
    {
        if (GetComponent<EquipmentSystem>() != null)
            return true;
        else
            return false;
    }

    public void addAllItemsToInventory()
    {
        for (int k = 0; k < ItemsInInventory.Count; k++)
        {
            for (int i = 0; i < SlotContainer.transform.childCount; i++)
            {
                if (SlotContainer.transform.GetChild(i).childCount == 0)
                {
                    GameObject item = (GameObject) Instantiate(prefabItem);
                    item.GetComponent<ItemOnObject>().item =
                        ItemsInInventory[k];
                    item
                        .transform
                        .SetParent(SlotContainer.transform.GetChild(i));
                    item.GetComponent<RectTransform>().localPosition =
                        Vector3.zero;
                    item.transform.GetChild(0).GetComponent<Image>().sprite =
                        ItemsInInventory[k].itemIcon;
                    break;
                }
            }
        }
        stackableSettings();
    }

    public bool checkIfItemAllreadyExist(int itemID, int itemValue)
    {
        updateItemList();
        int stack;
        for (int i = 0; i < ItemsInInventory.Count; i++)
        {
            if (ItemsInInventory[i].itemID == itemID)
            {
                stack = ItemsInInventory[i].itemValue + itemValue;
                if (stack <= ItemsInInventory[i].maxStack)
                {
                    ItemsInInventory[i].itemValue = stack;
                    GameObject temp = getItemGameObject(ItemsInInventory[i]);

                    // if (
                    //     temp != null &&
                    //     temp.GetComponent<ConsumeItem>().duplication != null
                    // )
                    //     temp
                    //         .GetComponent<ConsumeItem>()
                    //         .duplication
                    //         .GetComponent<ItemOnObject>()
                    //         .item
                    //         .itemValue = stack;
                    return true;
                }
            }
        }
        return false;
    }

    public void addItemToInventory(int id)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount == 0)
            {
                GameObject item = (GameObject) Instantiate(prefabItem);
                item.GetComponent<ItemOnObject>().item =
                    itemDatabase.getItemByID(id);
                item.transform.SetParent(SlotContainer.transform.GetChild(i));
                item.GetComponent<RectTransform>().localPosition = Vector3.zero;
                item.transform.GetChild(0).GetComponent<Image>().sprite =
                    item.GetComponent<ItemOnObject>().item.itemIcon;
                item.GetComponent<ItemOnObject>().item.indexItemInList =
                    ItemsInInventory.Count - 1;
                break;
            }
        }

        stackableSettings();
        updateItemList();
    }

    public GameObject addItemToInventory(int id, int value)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount == 0)
            {
                GameObject item = (GameObject) Instantiate(prefabItem);
                ItemOnObject itemOnObject = item.GetComponent<ItemOnObject>();
                itemOnObject.item = itemDatabase.getItemByID(id);
                if (
                    itemOnObject.item.itemValue <= itemOnObject.item.maxStack &&
                    value <= itemOnObject.item.maxStack
                )
                    itemOnObject.item.itemValue = value;
                else
                    itemOnObject.item.itemValue = 1;
                item.transform.SetParent(SlotContainer.transform.GetChild(i));
                item.GetComponent<RectTransform>().localPosition = Vector3.zero;
                item.transform.GetChild(0).GetComponent<Image>().sprite =
                    itemOnObject.item.itemIcon;
                itemOnObject.item.indexItemInList = ItemsInInventory.Count - 1;
                return item;
            }
        }

        stackableSettings();
        updateItemList();
        return null;
    }

    public void addItemToInventoryStorage(int itemID, int value)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount == 0)
            {
                GameObject item = (GameObject) Instantiate(prefabItem);
                ItemOnObject itemOnObject = item.GetComponent<ItemOnObject>();
                itemOnObject.item = itemDatabase.getItemByID(itemID);
                if (
                    itemOnObject.item.itemValue < itemOnObject.item.maxStack &&
                    value <= itemOnObject.item.maxStack
                )
                    itemOnObject.item.itemValue = value;
                else
                    itemOnObject.item.itemValue = 1;
                item.transform.SetParent(SlotContainer.transform.GetChild(i));
                item.GetComponent<RectTransform>().localPosition = Vector3.zero;
                itemOnObject.item.indexItemInList = 999;
                stackableSettings();
                break;
            }
        }
        stackableSettings();
        updateItemList();
    }

    public void stackableSettings(bool stackable, Vector3 posi)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount > 0)
            {
                ItemOnObject item =
                    SlotContainer
                        .transform
                        .GetChild(i)
                        .GetChild(0)
                        .GetComponent<ItemOnObject>();
                if (item.item.maxStack > 1)
                {
                    RectTransform textRectTransform =
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .GetChild(1)
                            .GetComponent<RectTransform>();
                    Text text =
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .GetChild(1)
                            .GetComponent<Text>();
                    text.text = "" + item.item.itemValue;
                    text.enabled = stackable;
                    textRectTransform.localPosition = posi;
                }
            }
        }
    }

    public void deleteAllItems()
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount != 0)
            {
                Destroy(SlotContainer
                    .transform
                    .GetChild(i)
                    .GetChild(0)
                    .gameObject);
            }
        }
    }

    public List<Item> getItemList()
    {
        List<Item> theList = new List<Item>();
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount != 0)
                theList
                    .Add(SlotContainer
                        .transform
                        .GetChild(i)
                        .GetChild(0)
                        .GetComponent<ItemOnObject>()
                        .item);
        }
        return theList;
    }

    public void stackableSettings()
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount > 0)
            {
                ItemOnObject item =
                    SlotContainer
                        .transform
                        .GetChild(i)
                        .GetChild(0)
                        .GetComponent<ItemOnObject>();
                if (item.item.maxStack > 1)
                {
                    RectTransform textRectTransform =
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .GetChild(1)
                            .GetComponent<RectTransform>();
                    Text text =
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .GetChild(1)
                            .GetComponent<Text>();
                    text.text = "" + item.item.itemValue;
                    text.enabled = stackable;
                    textRectTransform.localPosition =
                        new Vector3(positionNumberX, positionNumberY, 0);
                }
                else
                {
                    Text text =
                        SlotContainer
                            .transform
                            .GetChild(i)
                            .GetChild(0)
                            .GetChild(1)
                            .GetComponent<Text>();
                    text.enabled = false;
                }
            }
        }
    }

    public GameObject getItemGameObjectByName(Item item)
    {
        for (int k = 0; k < SlotContainer.transform.childCount; k++)
        {
            if (SlotContainer.transform.GetChild(k).childCount != 0)
            {
                GameObject itemGameObject =
                    SlotContainer.transform.GetChild(k).GetChild(0).gameObject;
                Item itemObject =
                    itemGameObject.GetComponent<ItemOnObject>().item;
                if (itemObject.itemName.Equals(item.itemName))
                {
                    return itemGameObject;
                }
            }
        }
        return null;
    }

    public GameObject getItemGameObject(Item item)
    {
        for (int k = 0; k < SlotContainer.transform.childCount; k++)
        {
            if (SlotContainer.transform.GetChild(k).childCount != 0)
            {
                GameObject itemGameObject =
                    SlotContainer.transform.GetChild(k).GetChild(0).gameObject;
                Item itemObject =
                    itemGameObject.GetComponent<ItemOnObject>().item;
                if (itemObject.Equals(item))
                {
                    return itemGameObject;
                }
            }
        }
        return null;
    }

    public void deleteItem(Item item)
    {
        for (int i = 0; i < ItemsInInventory.Count; i++)
        {
            if (item.Equals(ItemsInInventory[i]))
                ItemsInInventory.RemoveAt(item.indexItemInList);
        }
    }

    public void deleteItemFromInventory(Item item)
    {
        for (int i = 0; i < ItemsInInventory.Count; i++)
        {
            if (item.Equals(ItemsInInventory[i])) ItemsInInventory.RemoveAt(i);
        }
    }

    public void deleteItemFromInventoryWithGameObject(Item item)
    {
        for (int i = 0; i < ItemsInInventory.Count; i++)
        {
            if (item.Equals(ItemsInInventory[i]))
            {
                ItemsInInventory.RemoveAt (i);
            }
        }

        for (int k = 0; k < SlotContainer.transform.childCount; k++)
        {
            if (SlotContainer.transform.GetChild(k).childCount != 0)
            {
                GameObject itemGameObject =
                    SlotContainer.transform.GetChild(k).GetChild(0).gameObject;
                Item itemObject =
                    itemGameObject.GetComponent<ItemOnObject>().item;
                if (itemObject.Equals(item))
                {
                    Destroy (itemGameObject);
                    break;
                }
            }
        }
    }

    public int getPositionOfItem(Item item)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (SlotContainer.transform.GetChild(i).childCount != 0)
            {
                Item item2 =
                    SlotContainer
                        .transform
                        .GetChild(i)
                        .GetChild(0)
                        .GetComponent<ItemOnObject>()
                        .item;
                if (item.Equals(item2)) return i;
            }
        }
        return -1;
    }

    public void addItemToInventory(int ignoreSlot, int itemID, int itemValue)
    {
        for (int i = 0; i < SlotContainer.transform.childCount; i++)
        {
            if (
                SlotContainer.transform.GetChild(i).childCount == 0 &&
                i != ignoreSlot
            )
            {
                GameObject item = (GameObject) Instantiate(prefabItem);
                ItemOnObject itemOnObject = item.GetComponent<ItemOnObject>();
                itemOnObject.item = itemDatabase.getItemByID(itemID);
                if (
                    itemOnObject.item.itemValue < itemOnObject.item.maxStack &&
                    itemValue <= itemOnObject.item.maxStack
                )
                    itemOnObject.item.itemValue = itemValue;
                else
                    itemOnObject.item.itemValue = 1;
                item.transform.SetParent(SlotContainer.transform.GetChild(i));
                item.GetComponent<RectTransform>().localPosition = Vector3.zero;
                itemOnObject.item.indexItemInList = 999;
                stackableSettings();
                break;
            }
        }
        stackableSettings();
        updateItemList();
    }

    public void updateItemIndex()
    {
        for (int i = 0; i < ItemsInInventory.Count; i++)
        {
            ItemsInInventory[i].indexItemInList = i;
        }
    }
}
