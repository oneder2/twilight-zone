using Unity.VisualScripting;
using UnityEngine;

public class NPC : Interactable
{
    [SerializeField] private string[] dialogueLines;

    public override void Interact()
    {
        Player.Instance.ChangeStateTo(Player.Instance.idleState);
        GameManager.Instance.isInDialogue = true;  // 进入对话状态，暂停时间
        DialogueGUI.Instance.ShowDialogue(dialogueLines);
    }

    public override string GetDialogue()
    {
        return "按 E 与 " + gameObject.name + " 对话";
    }
}