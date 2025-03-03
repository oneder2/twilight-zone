using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;      // 物品名称
    public Sprite icon;      // 物品图标
    // 可根据需要添加其他属性，例如价值、类型等
}