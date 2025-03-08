using UnityEngine;

public static class ItemHelper
{
    public static Item GetItemByUniqueID(string uniqueID)
    {
        Item[] items = Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
        foreach (Item item in items)
        {
            if (item.uniqueID == uniqueID)
            {
                return item;
            }
        }
        return null; // 未找到则返回 null
    }
}