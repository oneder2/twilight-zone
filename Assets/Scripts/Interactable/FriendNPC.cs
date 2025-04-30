using UnityEngine;

// In
public class FriendNPC : Interactable
{
    [SerializeField] private string[] dialogueLines;

    public override void Interact()
    {
        GameRunManager.Instance.ChangeGameStatus(GameStatus.InDialogue);
        GameManager.Instance.StartDialogue();
        DialogueManager.Instance.ShowDialogue(dialogueLines);
    }
}