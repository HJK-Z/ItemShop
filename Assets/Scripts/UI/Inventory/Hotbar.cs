using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Linq;

public class Hotbar : MonoBehaviour
{

    [SerializeField]
    public KeyCode[] keyCodesForSlots = new KeyCode[999];
    [SerializeField]
    public int slotsInTotal;

    void Update()
    {
        // for (int i = 0; i < slotsInTotal; i++)
        // {
        //     if (Input.GetKeyDown(keyCodesForSlots[i]))
        //     {
        //         if (transform.GetChild(1).GetChild(i).childCount != 0 && transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<ItemOnObject>().item.itemType != ItemType.UFPS_Ammo)
        //         {
        //             if (transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<ConsumeItem>().duplication != null && transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<ItemOnObject>().item.maxStack == 1)
        //             {
        //                 Destroy(transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<ConsumeItem>().duplication);
        //             }
        //             transform.GetChild(1).GetChild(i).GetChild(0).GetComponent<ConsumeItem>().consumeIt();
        //         }
        //     }
        // }
    }

    public int getSlotsInTotal()
    {
        Inventory inv = GetComponent<Inventory>();
        return slotsInTotal = inv.width * inv.height;
    }
}
