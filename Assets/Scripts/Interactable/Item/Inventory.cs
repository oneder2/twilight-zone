using System.Collections.Generic;
using UnityEngine;

public class Inventory : Singleton<Inventory>
{
    private List<ItemData> items = new List<ItemData>();

    // 添加道具
    public void AddItem(ItemData itemData)
    {
        items.Add(itemData);
    }

    // 移除道具
    public void RemoveItem(ItemData itemData)
    {
        items.Remove(itemData);
    }

    // 获取所有道具
    public List<ItemData> GetItemDatas()
    {
        return items;
    }
}