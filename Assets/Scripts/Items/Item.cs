using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Item
{
    public string itemName;                                     
    public int itemID;                             
    public string itemDesc;                         
    public Sprite itemIcon;                          
    public int itemStack = 1;                
    public ItemType itemType;         
    public int maxStack = 1;
    public int indexItemInList = 999;  

    [SerializeField]
    public List<Effect> itemAttributes = new List<Effect>();    
    
    public Item()
    {
        itemID = -1;
    }

    public Item(string name, int id, string desc, Sprite icon, int maxStack, ItemType type, string sendmessagetext, List<Effect> itemAttributes)
    {
        itemName = name;
        itemID = id;
        itemDesc = desc;
        itemIcon = icon;
        itemType = type;
        this.maxStack = maxStack;
        this.itemAttributes = itemAttributes;
    }

    public Item getCopy()
    {
        return (Item)this.MemberwiseClone();        
    }   
    
    
}


