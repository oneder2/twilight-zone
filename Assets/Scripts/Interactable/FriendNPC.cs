using UnityEngine;

// In
public class FriendNPC : Interactable
{
    [SerializeField] private string[] dialogueLines;

    public override void Interact()
    {
        Player.Instance.ChangeStateTo(Player.Instance.idleState);
        GameManager.Instance.isInDialogue = true;  // 进入对话状态，暂停时间
        DialogueGUI.Instance.ShowDialogue(dialogueLines);
    }
}