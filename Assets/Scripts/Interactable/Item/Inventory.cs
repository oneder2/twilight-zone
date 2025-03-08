using System.Collections.Generic;
using UnityEngine;

public class Inventory : Singleton<Inventory>
{
    public static Inventory instance;  // 单例实例
    public List<ItemData> items = new List<ItemData>();  // 物品列表

    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log("已添加 " + item.name + " 到库存");
    }
}