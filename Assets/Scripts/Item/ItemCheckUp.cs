using UnityEngine;

public class ItemCheckUp : Interactable
{
    public Item item;  // 在 Inspector 中关联具体的物品数据
    [SerializeField] private string dialogue = "I check this thing"; // 在Inspector中设置评价文字

    public override void Interact()
    {
        // 显示评价文字
        DialogueGUI.canvas.ShowDialogue(dialogue);
    }

    public override string GetDialogue()
    {
        return dialogue;
    }
}