using UnityEngine;

public class ItemPickup : Item
{
    public override void Interact()
    {
        GameManager.Instance.isInteracting = true;  // Marted as interacting
        
        // Add item to inventory
        Inventory.Instance.AddItem(itemData);
        // Arouse ItemPickUp event
        EventManager.Instance.TriggerEvent(new ItemPickedUpEvent(itemData.name));
        // Visualize comment text
        DialogueGUI.Instance.ShowDialogue(itemData.discribe);
        // Destroy object in scene
        Destroy(gameObject);

        GameManager.Instance.isInteracting = false;  // Immidiatly ends the interaction
    }

    public override bool UseItem(ItemData usedItemData)
    {
        // Some items need be triggered to use
        return false; // Default as unsuable
    }
}