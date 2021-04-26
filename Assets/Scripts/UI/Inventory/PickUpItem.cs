using System.Collections;
using UnityEngine;

public class PickUpItem : MonoBehaviour
{
    public Item item;

    private Inventory _inventory;

    private Inventory _hotbar;

    private GameObject _player;

    // Use this for initialization
    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player != null)
            _inventory = _player.GetComponent<PlayerInventory>().inventory;
        _hotbar = _player.GetComponent<PlayerInventory>().hotbar;
    }

    // Update is called once per frame
    void Update()
    {
        float distance =
            Vector3
                .Distance(this.gameObject.transform.position,
                _player.transform.position);

        if (distance <= 2)
        {
            bool check =
                _inventory.checkIfItemExists(item.itemID, item.itemStack);
            if (check)
                Destroy(this.gameObject);
            else if (
                _inventory.ItemsInInventory.Count <
                (_inventory.width * _inventory.height)
            )
            {
                _inventory.addItemToInventory(item.itemID, item.itemStack);
                _inventory.updateItemList();
                _inventory.stackableSettings();
                Destroy(this.gameObject);
            }
            else
            {
                check = _hotbar.checkIfItemExists(item.itemID, item.itemStack);
                if (check)
                    Destroy(this.gameObject);
                else if (
                    _hotbar.ItemsInInventory.Count <
                    (_hotbar.width * _hotbar.height)
                )
                {
                    _hotbar.addItemToInventory(item.itemID, item.itemStack);
                    _hotbar.updateItemList();
                    _hotbar.stackableSettings();
                    Destroy(this.gameObject);
                }
            }
        }
    }
}
