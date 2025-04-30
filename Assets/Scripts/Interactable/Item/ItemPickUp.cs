using UnityEngine;

public class ItemPickup : Item
{
    
    public override void Interact()
    {
        // GameManager.Instance.isInteracting = true; // Consider if this flag is still needed

        // Add item to player's inventory
        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(itemData);
        } else { Debug.LogError("Inventory instance not found!"); }

        // Trigger pickup event
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(new ItemPickedUpEvent(itemData.name)); // Use your actual event class
        } else { Debug.LogError("EventManager instance not found!"); }

        // Show pickup dialogue
        if (DialogueManager.Instance != null)
        {
             DialogueManager.Instance.ShowDialogue($"Picked up: {itemData.itemName}"); // Use itemData.itemName for clarity
        } else { Debug.LogError("DialogueManager instance not found!"); }


        // --- Notify GameSceneManager BEFORE destroying ---
        if (ownerSceneManager != null) // ownerSceneManager should be set in base Item's Start()
        {
            ownerSceneManager.NotifyItemPickedUp(this.uniqueID);
        }
        else
        {
             Debug.LogWarning($"ItemPickup '{uniqueID}' could not notify its GameSceneManager before destruction.");
        }
        // --- End Notification ---

        // Destroy the item GameObject from the scene
        Destroy(gameObject);

        // GameManager.Instance.isInteracting = false;
    }

    public override bool UseItem(ItemData usedItemData)
    {
        // Some items need be triggered to use
        return false; // Default as unsuable
    }
}