using UnityEngine;

public class ItemPickup : Item
{
    [SerializeField] private string dialogue = "This thing seems useful"; // 在Inspector中设置评价文字

    public override void Interact()
    {
        // 将物品添加到库存
        Inventory.instance.AddItem(itemData);
        // 显示评价文字
        DialogueGUI.Instance.ShowDialogue(dialogue);
        // 销毁场景中的物品对象
        Destroy(gameObject);
    }

    public override string GetDialogue()
    {
        return dialogue;
    }
}