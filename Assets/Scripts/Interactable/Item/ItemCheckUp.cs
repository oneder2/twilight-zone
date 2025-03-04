using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUp : Item
{
    [SerializeField] private string dialogue = "I check this thing"; // 在Inspector中设置评价文字

    public override void Interact()
    {
        // 显示评价文字
        DialogueGUI.Instance.ShowDialogue(dialogue);
    }

    public override string GetDialogue()
    {
        return dialogue;
    }
}