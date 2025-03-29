using UnityEngine;

// In
public class FriendNPC : Interactable
{
    [SerializeField] private string[] dialogueLines;

    public override void Interact()
    {
        Player.Instance.ChangeStateTo(Player.Instance.idleState);
        GameManager.Instance.StartDialogue();
        DialogueGUI.Instance.ShowDialogue(dialogueLines);
    }
}