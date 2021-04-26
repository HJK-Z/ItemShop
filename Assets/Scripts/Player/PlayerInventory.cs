using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    public PlayerCharacterController player;

    public Inventory inventory;

    public Inventory equipmentSystem;

    public Inventory hotbar;

    private Tooltip toolTip;

    void Start()
    {
        if (GameObject.FindGameObjectWithTag("Tooltip") != null)
            toolTip =
                GameObject
                    .FindGameObjectWithTag("Tooltip")
                    .GetComponent<Tooltip>();
    }

    public void OnConsumeItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            player.gameObject.GetComponent<Affectable>().effects.Add(item.itemAttributes[i]);
        }
    }

    public void OnGearItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            // if (item.itemAttributes[i].attributeName == "Health")
            //     maxHealth += item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Mana")
            //     maxMana += item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Armor")
            //     maxArmor += item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Damage")
            //     maxDamage += item.itemAttributes[i].attributeValue;
        }
    }

    public void OnUnEquipItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            // if (item.itemAttributes[i].attributeName == "Health")
            //     maxHealth -= item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Mana")
            //     maxMana -= item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Armor")
            //     maxArmor -= item.itemAttributes[i].attributeValue;
            // if (item.itemAttributes[i].attributeName == "Damage")
            //     maxDamage -= item.itemAttributes[i].attributeValue;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetState(bool open)
    {
        if (open)
        {
            inventory.openInventory();
            equipmentSystem.openInventory();
        }
        else
        {
            if (toolTip != null) toolTip.deactivateTooltip();
            inventory.closeInventory();
            equipmentSystem.closeInventory();
        }
    }
}
