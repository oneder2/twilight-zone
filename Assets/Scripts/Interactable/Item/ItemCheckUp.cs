using Unity.VisualScripting;
using UnityEngine;

public class ItemCheckUp : Item
{    
    [Tooltip("Tracks if this item has been checked/interacted with.")]
    public bool hasBeenChecked = false; // This state needs to be saved/loaded

    public override void Interact()
    {
        // GameManager.Instance.isInteracting = true;

        string[] linesToShow;
        if (!hasBeenChecked)
        {
            // Use the longer comments/dialogue on first check
            linesToShow = itemData.commends; // Assuming 'commends' holds the detailed text
            hasBeenChecked = true; // Mark as checked *after* interaction
            Debug.Log($"Item '{uniqueID}' marked as checked.");
            // Note: GameSceneManager will save the 'hasBeenChecked = true' state next time.
        }
        else
        {
            // Use the shorter description on subsequent checks
            linesToShow = new string[] { itemData.discribe }; // Assuming 'discribe' is the short text
        }

        if (DialogueGUI.Instance != null)
        {
             DialogueGUI.Instance.ShowDialogue(linesToShow);
        } else { Debug.LogError("DialogueGUI instance not found!"); }

        // GameManager.Instance.isInteracting = false;
    }

     // Method to directly set the checked state, called by GameSceneManager.LoadSaveData
     public void SetCheckedState(bool isChecked)
     {
          this.hasBeenChecked = isChecked;
          // Optional: Update visuals immediately if needed based on the loaded state
          // e.g., change sprite, show/hide an indicator
          Debug.Log($"Item '{uniqueID}' checked state set to {isChecked} from loaded data.");
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