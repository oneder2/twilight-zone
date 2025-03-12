using UnityEngine;

public class ItemPickup : Item
{
    public override void Interact()
    {
        GameManager.instance.isInteracting = true;  // 标记为交互中，但不暂停时间
        
        // 将物品添加到库存
        Inventory.Instance.AddItem(itemData);
        // 显示评价文字
        DialogueGUI.Instance.ShowDialogue(itemData.commend);
        // 销毁场景中的物品对象
        Destroy(gameObject);

        GameManager.instance.isInteracting = false;  // 交互立即结束
    }

    public override string GetDialogue()
    {
        return itemData.commend;
    }
}