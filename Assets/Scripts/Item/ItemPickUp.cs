using UnityEngine;

public class ItemPickup : Interactable
{
    public Item item;  // 在 Inspector 中关联具体的物品数据
    [SerializeField] private string dialogue = "This thing seems useful"; // 在Inspector中设置评价文字

    public override void Interact()
    {
        // 将物品添加到库存
        Inventory.instance.AddItem(item);
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