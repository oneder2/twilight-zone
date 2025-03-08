using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUp : Item
{
    public bool hasBeenChecked = false;
    
    public override void Interact()
    {
        if (!hasBeenChecked)
        {
            DialogueGUI.Instance.ShowDialogue(itemData.commend);
            hasBeenChecked = true;
        }
        DialogueGUI.Instance.ShowDialogue("...");
    }

    public override string GetDialogue()
    {
        return itemData.commend;
    }
}