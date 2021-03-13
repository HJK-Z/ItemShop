using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    public PlayerCharacterController player;

    public GameObject inventory;

    public GameObject characterSystem;

    private Inventory mainInventory;

    private Inventory characterSystemInventory;

    private Tooltip toolTip;

    void Start()
    {
        if (GameObject.FindGameObjectWithTag("Tooltip") != null)
            toolTip =
                GameObject
                    .FindGameObjectWithTag("Tooltip")
                    .GetComponent<Tooltip>();
        if (inventory != null)
            mainInventory = inventory.GetComponent<Inventory>();
        if (characterSystem != null)
            characterSystemInventory =
                characterSystem.GetComponent<Inventory>();
    }

    public void OnConsumeItem(Item item)
    {
        for (int i = 0; i < item.itemAttributes.Count; i++)
        {
            // if (item.itemAttributes[i].attributeName == "Health")
            // {
            //     if (
            //         (currentHealth + item.itemAttributes[i].attributeValue) >
            //         maxHealth
            //     )
            //         currentHealth = maxHealth;
            //     else
            //         currentHealth += item.itemAttributes[i].attributeValue;
            // }
            // if (item.itemAttributes[i].attributeName == "Mana")
            // {
            //     if (
            //         (currentMana + item.itemAttributes[i].attributeValue) >
            //         maxMana
            //     )
            //         currentMana = maxMana;
            //     else
            //         currentMana += item.itemAttributes[i].attributeValue;
            // }
            // if (item.itemAttributes[i].attributeName == "Armor")
            // {
            //     if (
            //         (currentArmor + item.itemAttributes[i].attributeValue) >
            //         maxArmor
            //     )
            //         currentArmor = maxArmor;
            //     else
            //         currentArmor += item.itemAttributes[i].attributeValue;
            // }
            // if (item.itemAttributes[i].attributeName == "Damage")
            // {
            //     if (
            //         (currentDamage + item.itemAttributes[i].attributeValue) >
            //         maxDamage
            //     )
            //         currentDamage = maxDamage;
            //     else
            //         currentDamage += item.itemAttributes[i].attributeValue;
            // }
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

    public void SetState(bool open){
        if (open)
        {
            mainInventory.openInventory();
            characterSystemInventory.openInventory();
        }
        else
        {
            if (toolTip != null) toolTip.deactivateTooltip();
            mainInventory.closeInventory();
            characterSystemInventory.closeInventory();
        }
    }
}
