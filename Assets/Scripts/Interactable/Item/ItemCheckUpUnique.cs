using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUpUnique : Item
{
    public bool hasBeenChecked = false;
    [SerializeField] private string[] dialogueLines;
    
    public override void Interact()
    {
        GameManager.Instance.isInteracting = true;  // 标记为交互中，但不暂停时间

        DialogueGUI.Instance.ShowDialogue(dialogueLines);

        GameManager.Instance.isInteracting = false;  // 交互立即结束
    }
}