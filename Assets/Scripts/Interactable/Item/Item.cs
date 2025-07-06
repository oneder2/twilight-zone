using UnityEngine;
using UnityEngine.SceneManagement;

public class Item : Interactable
{

    [Tooltip("Data asset containing item details like name, icon, description.")]
    public ItemData itemData; // Assign in Inspector

    [Tooltip("A unique identifier for this specific item instance within the scene. MUST BE UNIQUE per scene.")]
    public string uniqueID; // Assign a unique ID in the Inspector for each savable item instance

    protected GameSceneManager ownerSceneManager; // Reference to the manager of the scene this item is in

        // --- In Item.cs ---
    protected override void Start() // Assuming override from previous fix
    {
        base.Start(); // Call Interactable.Start()

        // --- Robust Item Registration ---
        ownerSceneManager = null; // Reset reference first
        Scene currentScene = gameObject.scene; // Get the scene this GameObject belongs to

        if (currentScene.IsValid() && currentScene.isLoaded)
        {
            GameObject[] rootObjects = currentScene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                // Search within the root objects of this specific scene
                GameSceneManager managerInScene = root.GetComponentInChildren<GameSceneManager>(true); // Include inactive
                if (managerInScene != null)
                {
                    ownerSceneManager = managerInScene;
                    break; // Found it in this scene
                }
            }
        }

        // Now register if found
        if (ownerSceneManager != null)
        {
            // Optional but good: double-check the found manager is indeed in the same scene
            if (ownerSceneManager.gameObject.scene == currentScene)
            {
                ownerSceneManager.RegisterItem(this);
                // Debug.Log($"Item '{uniqueID}' registered with GameSceneManager in scene '{currentScene.name}'.");
            }
            else
            {
                 // This case should ideally not happen with the search logic above
                 Debug.LogError($"Item '{uniqueID}' found a GameSceneManager, but it belongs to a different scene ('{ownerSceneManager.gameObject.scene.name}')! Registration aborted.", gameObject);
                 ownerSceneManager = null;
            }
        }
        else
        {
            Debug.LogError($"Item '{uniqueID}' could not find GameSceneManager within its own scene ('{currentScene.name}')!", gameObject);
        }
        // --- End Robust Item Registration ---
    }
    

     // Optional: Unregister on destroy to keep the manager's list clean,
     // although GameSceneManager will be destroyed with the scene anyway.
     protected virtual void OnDestroy()
     {
          if (ownerSceneManager != null)
          {
               // ownerSceneManager.UnregisterItem(this); // Need to implement UnregisterItem if using this
          }
     }

}