using UnityEngine;

public class ItemPickup : Item
{
    public override void Interact()
    {
        // 将物品添加到库存
        Inventory.Instance.AddItem(itemData);
        // 显示评价文字
        DialogueGUI.Instance.ShowDialogue(itemData.commend);
        // 销毁场景中的物品对象
        Destroy(gameObject);
    }

    public override string GetDialogue()
    {
        return itemData.commend;
    }
}