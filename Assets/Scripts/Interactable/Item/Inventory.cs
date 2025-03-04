using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;  // 单例实例
    public List<ItemData> items = new List<ItemData>();  // 物品列表

    void Awake()
    {
        // 初始化单例
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log("已添加 " + item.name + " 到库存");
    }
}