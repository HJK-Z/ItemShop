using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class EquipmentSystem : MonoBehaviour
{
    [SerializeField]
    public int slotsInTotal;
    [SerializeField]
    public ItemType[] itemTypeOfSlots = new ItemType[999];

    void Start()
    {
        ConsumeItem.eS = GetComponent<EquipmentSystem>();
    }

    public void getSlotsInTotal()
    {
        Inventory inv = GetComponent<Inventory>();
        slotsInTotal = inv.width * inv.height;
    }

}

