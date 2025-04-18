using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for finding root objects efficiently if using that method

// Assuming definitions from DataStructure.cs are available
// public class SceneSaveData { public List<ItemStatePair> itemsState; }
// public class ItemStatePair { public string uniqueID; public ItemSaveData itemState; }
// public class ItemSaveData { public string type; public bool isPresent; public bool hasBeenChecked; }

/// <summary>
/// Manages the state of interactable items within this specific scene.
/// Handles item registration, saving the current state of registered items,
/// and loading/applying previously saved state for this scene.
/// There should be one instance of this manager per scene that needs state saved.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Tooltip("The unique name of this scene. Should match the scene file name exactly.")]
    [SerializeField] public string sceneName; // Assign in Inspector, crucial for SessionStateManager key

    // --- Item Registration ---
    // Stores references to Item components in this scene, keyed by their uniqueID.
    private Dictionary<string, Item> registeredItems = new Dictionary<string, Item>();
    // Keep track of items that were initially configured but have been picked up (if needed for save logic)
    private HashSet<string> pickedUpItemIDs = new HashSet<string>();

    void Awake()
    {
        // Validate scene name assignment
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"GameSceneManager on GameObject '{gameObject.name}' is missing its Scene Name!", this.gameObject);
        }
        // Clear registration in case of reloads (though usually scene unload/load handles this)
        registeredItems.Clear();
        pickedUpItemIDs.Clear();
    }

    /// <summary>
    /// Called by Item scripts in their Start() method to register themselves.
    /// </summary>
    /// <param name="item">The item component registering itself.</param>
    public void RegisterItem(Item item)
    {
        if (item == null || string.IsNullOrEmpty(item.uniqueID))
        {
            Debug.LogError("Attempted to register an invalid item.", item?.gameObject);
            return;
        }

        if (!registeredItems.ContainsKey(item.uniqueID))
        {
            registeredItems.Add(item.uniqueID, item);
            // Debug.Log($"Registered item: {item.uniqueID}");
        }
        else
        {
            // This might happen if IDs are not unique or due to editor duplication.
            Debug.LogWarning($"Duplicate uniqueID registration attempt: {item.uniqueID} in scene {sceneName}. GameObject: {item.gameObject.name}", item.gameObject);
        }
    }

    /// <summary>
    /// Called by ItemPickup scripts *before* they destroy themselves.
    /// Marks the item as picked up so its state is saved correctly.
    /// </summary>
    /// <param name="uniqueID">The unique ID of the item being picked up.</param>
    public void NotifyItemPickedUp(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID)) return;

        pickedUpItemIDs.Add(uniqueID); // Mark as picked up
        // Also remove from the active registry as the GameObject will be destroyed
        if (registeredItems.ContainsKey(uniqueID))
        {
            registeredItems.Remove(uniqueID);
        }
        Debug.Log($"Item '{uniqueID}' marked as picked up in scene '{sceneName}'.");
    }


    /// <summary>
    /// Collects the current state of all relevant items in this scene.
    /// Called by TransitionManager before the scene is unloaded.
    /// </summary>
    /// <returns>A SceneSaveData object containing the state of items in this scene.</returns>
    public SceneSaveData SaveCurrentState()
    {
        Debug.Log($"Saving state for scene: {sceneName}");
        SceneSaveData sceneSaveData = new SceneSaveData { itemsState = new List<ItemStatePair>() };

        // We need a definitive list of ALL items that *could* exist and need saving.
        // Using the initial configuration (itemUniqueIDs/itemTypePairs from your original script)
        // might be necessary if items can be destroyed. Let's assume we iterate registered items
        // AND consider picked up items.

        // Save state of currently registered (existing) items
        foreach (var pair in registeredItems)
        {
            string uniqueID = pair.Key;
            Item item = pair.Value;
            if (item == null) continue; // Should not happen if registration is clean, but safety check

            ItemSaveData itemSaveData = new ItemSaveData { isPresent = true }; // It's registered, so it's present

            // Determine type and save relevant state
            if (item is ItemCheckUp checkUp)
            {
                itemSaveData.type = "ItemCheckUp";
                itemSaveData.hasBeenChecked = checkUp.hasBeenChecked;
            }
            // Add checks for other savable item types here...
            // else if (item is SomeOtherSavableItemType savableItem) { ... }
            else
            {
                // If it's just a base Item or an ItemPickup that somehow wasn't destroyed yet
                itemSaveData.type = item.GetType().Name; // Store the actual type name
                // Add any other relevant state for base items if needed
            }

            sceneSaveData.itemsState.Add(new ItemStatePair { uniqueID = uniqueID, itemState = itemSaveData });
        }

        // Add entries for items that were picked up (to explicitly mark them as not present)
        foreach (string pickedUpID in pickedUpItemIDs)
        {
            // We only need to add this if it wasn't already processed above (it shouldn't be, as it was removed from registeredItems)
             if (!sceneSaveData.itemsState.Any(statePair => statePair.uniqueID == pickedUpID))
             {
                  ItemSaveData itemSaveData = new ItemSaveData
                  {
                       type = "ItemPickup", // Assume only ItemPickups get added to pickedUpItemIDs
                       isPresent = false,
                       hasBeenChecked = false // Not relevant for pickups
                  };
                  sceneSaveData.itemsState.Add(new ItemStatePair { uniqueID = pickedUpID, itemState = itemSaveData });
             }
        }


        Debug.Log($"Saved state for {sceneSaveData.itemsState.Count} items in scene: {sceneName}");
        return sceneSaveData;
    }

    /// <summary>
    /// Applies a previously saved state to the items in this scene.
    /// Called by TransitionManager after this scene has loaded.
    /// </summary>
    /// <param name="sceneSaveData">The state data retrieved from SessionStateManager.</param>
    public void LoadSaveData(SceneSaveData sceneSaveData)
    {
        if (sceneSaveData == null || sceneSaveData.itemsState == null)
        {
            Debug.LogWarning($"LoadSaveData called with null data for scene: {sceneName}. Skipping load.");
            return;
        }
        Debug.Log($"Loading state for scene: {sceneName} with {sceneSaveData.itemsState.Count} item states.");

        // Create a temporary list of items to destroy to avoid modifying dictionary while iterating
        List<Item> itemsToDestroy = new List<Item>();

        foreach (ItemStatePair pair in sceneSaveData.itemsState)
        {
            if (string.IsNullOrEmpty(pair.uniqueID) || pair.itemState == null)
            {
                Debug.LogWarning("Skipping invalid ItemStatePair in loaded data.");
                continue;
            }

            // Try to find the item instance currently present in the scene
            if (registeredItems.TryGetValue(pair.uniqueID, out Item item))
            {
                // Item exists in the scene, apply its saved state
                if (!pair.itemState.isPresent)
                {
                    // Saved state says item should NOT be present (e.g., it was picked up)
                    Debug.Log($"Item '{item.uniqueID}' state loaded as 'not present'. Marking for destruction.");
                    itemsToDestroy.Add(item); // Mark for destruction after loop
                    pickedUpItemIDs.Add(item.uniqueID); // Ensure our internal tracking matches loaded state
                }
                else
                {
                    // Item should be present, apply specific state based on type
                    if (item is ItemCheckUp checkUp && pair.itemState.type == "ItemCheckUp")
                    {
                        checkUp.SetCheckedState(pair.itemState.hasBeenChecked); // Use a dedicated method if needed
                    }
                    // Add loading logic for other savable item types here...
                    // else if (item is SomeOtherSavableItemType savableItem && pair.itemState.type == "SomeOtherSavableItemType") { ... }

                    // Ensure it's not marked as picked up if loaded state says isPresent=true
                    pickedUpItemIDs.Remove(item.uniqueID);
                }
            }
            else
            {
                // Item from save data not found among currently registered items.
                // This is expected if isPresent is false (it was picked up and destroyed).
                if (!pair.itemState.isPresent)
                {
                    // Correctly reflects a picked-up item. Ensure it's tracked.
                    pickedUpItemIDs.Add(pair.uniqueID);
                    // Debug.Log($"Item '{pair.uniqueID}' correctly not found (was picked up).");
                }
                else
                {
                    // Saved state says item should be present, but it wasn't registered/found.
                    // This might indicate an issue: item prefab missing, uniqueID mismatch, or registration failed.
                    Debug.LogWarning($"Item with uniqueID '{pair.uniqueID}' expected to be present but not found in registered items for scene '{sceneName}' during load.");
                }
            }
        }

        // Now, destroy the items marked for destruction
        foreach (Item itemToDestroy in itemsToDestroy)
        {
            if (itemToDestroy != null)
            {
                Debug.Log($"Destroying item '{itemToDestroy.uniqueID}' based on loaded state.");
                registeredItems.Remove(itemToDestroy.uniqueID); // Unregister before destroying
                Destroy(itemToDestroy.gameObject);
            }
        }
        Debug.Log($"Finished applying state for scene: {sceneName}");
    }
}
