using UnityEngine;

public class ItemPickup : Interactable
{
    public Item item;  // 在 Inspector 中关联具体的物品数据

    public override void Interact()
    {
        // 将物品添加到库存
        Inventory.instance.AddItem(item);
        // 销毁场景中的物品对象
        Destroy(gameObject);
    }
}