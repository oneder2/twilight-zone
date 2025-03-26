using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUp : Item
{
    public bool hasBeenChecked = false;
    
    public override void Interact()
    {
        GameManager.Instance.isInteracting = true;  // check as interacting, without pause time

        if (!hasBeenChecked)
        {
            // If havn't been checked, output a longer commends
            DialogueGUI.Instance.ShowDialogue(itemData.commends);
            hasBeenChecked = true;
        }
        else
        {
            // After interaction, interacte with item only show simple discription
            DialogueGUI.Instance.ShowDialogue(itemData.discribe);
        }

        GameManager.Instance.isInteracting = false;  // interaction ends immidiatly
    }

    public override bool UseItem(ItemData usedItemData)
    {
        // 示例：使用特定道具触发新对话
        if (usedItemData.itemName == "Key" && this.itemData.itemName == "LockedBox")
        {
            DialogueGUI.Instance.ShowDialogue(new string[] { "The box is unlocked!" });
            return true;
        }
        return false;
    }
}