using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDataBaseList : MonoBehaviour
{
    // where the Item getting stored which you create(ItemDatabase)
    [SerializeField]
    public List<Item> itemList = new List<Item>(); //List of it

    [SerializeField]
    public GameObject pickupItem;

    public void Start()
    {
        itemList
            .Add(new Item("Bread",
                0,
                "eat lmao",
                Resources.Load<Sprite>("I_C_Bread"),
                pickupItem,
                5,
                ItemType.Consumable,
                "",
                new List<Effect>()));
        itemList
            .Add(new Item("\"Bread\"",
                1,
                "actually a gun",
                Resources.Load<Sprite>("I_C_Bread"),
                pickupItem,
                1,
                ItemType.Weapon,
                "",
                new List<Effect>()));
    }

    public Item getItemByID(int id)
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            if (itemList[i].itemID == id) return itemList[i].getCopy();
        }
        return null;
    }

    public Item getItemByName(string name)
    {
        for (int i = 0; i < itemList.Count; i++)
        {
            if (itemList[i].itemName.ToLower().Equals(name.ToLower()))
                return itemList[i].getCopy();
        }
        return null;
    }
}
