using System.Collections.Generic;
using UnityEngine;

public class Inventory : Singleton<Inventory>
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    // Add items
    public void AddItem(ItemData itemData)
    {
        items.Add(itemData);
    }

    // Remove items
    public void RemoveItem(ItemData itemData)
    {
        items.Remove(itemData);
    }

    // Get access to all the items
    public List<ItemData> GetItemDatas()
    {
        return items;
    }

    public void Clear()
    {
        items = new List<ItemData>();
    }
}