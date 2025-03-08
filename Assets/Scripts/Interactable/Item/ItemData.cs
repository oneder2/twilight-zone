using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;      // 物品名称
    public Sprite icon;      // 物品图标
    public string commend;
    // 可根据需要添加其他属性，例如价值、类型等
}