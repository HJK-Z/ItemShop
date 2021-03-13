using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Affectable : MonoBehaviour
{
    [SerializeField]
    public List<ItemAttribute> itemAttributes = new List<ItemAttribute>();

    public Affectable()
    {
    }

    public Item getCopy()
    {
        return (Item) this.MemberwiseClone();
    }
}
