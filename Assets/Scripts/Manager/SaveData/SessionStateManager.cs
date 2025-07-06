using UnityEngine;
using System.Collections.Generic;

// Assuming SceneSaveData is defined in your DataStructure.cs
// [System.Serializable]
// public class SceneSaveData { ... }

/// <summary>
/// Manages the state of interactable objects within scenes for the current game session.
/// Stores scene states in memory, allowing state persistence when returning to previously visited scenes
/// during a single playthrough (does not save to disk).
/// Should reside in the persistent GameRoot scene.
/// </summary>
public class SessionStateManager : Singleton<SessionStateManager> // Use your Singleton base class
{
    // Dictionary to hold the last known state of each scene visited in this session.
    // Key: Scene Name (string)
    // Value: SceneSaveData object containing item states for that scene.
    private Dictionary<string, SceneSaveData> sceneStates = new Dictionary<string, SceneSaveData>();

    /// <summary>
    /// Records or updates the state data for a specific scene.
    /// Typically called by TransitionManager before a scene is unloaded.
    /// </summary>
    /// <param name="sceneName">The name of the scene whose state is being saved.</param>
    /// <param name="data">The state data collected from the scene's GameSceneManager.</param>
    public void RecordSceneState(string sceneName, SceneSaveData data)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SessionStateManager] Attempted to record state for an invalid scene name.");
            return;
        }
        if (data == null)
        {
             Debug.LogWarning($"[SessionStateManager] Received null SceneSaveData for scene: {sceneName}. Storing null state.");
             // Decide if storing null is intended or should be an error/ignored.
        }

        sceneStates[sceneName] = data; // Add or overwrite the state for this scene.
        Debug.Log($"[SessionStateManager] State recorded for scene: '{sceneName}'.");
    }

    /// <summary>
    /// Attempts to retrieve the previously recorded state data for a specific scene.
    /// Typically called by TransitionManager after a scene has loaded.
    /// </summary>
    /// <param name="sceneName">The name of the scene to retrieve state for.</param>
    /// <param name="data">Output parameter for the retrieved state data (null if not found).</param>
    /// <returns>True if state data was found for the scene, false otherwise.</returns>
    public bool TryGetSceneState(string sceneName, out SceneSaveData data)
    {
        data = null;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SessionStateManager] Attempted to get state for an invalid scene name.");
            return false;
        }
        bool found = sceneStates.TryGetValue(sceneName, out data);
        if (found)
        {
            Debug.Log($"[SessionStateManager] Found saved state for scene: '{sceneName}'.");
        }
        else
        {
            Debug.Log($"[SessionStateManager] No saved state found for scene: '{sceneName}'. Scene will initialize in default state.");
        }
        return found;
    }

    /// <summary>
    /// Clears all stored scene states from memory.
    /// Should be called when the entire game session ends (e.g., returning to the main menu).
    /// </summary>
    public void ClearAllSceneStates()
    {
        sceneStates.Clear();
        Debug.Log("[SessionStateManager] All stored scene states cleared.");
    }

    // Optional: Automatically clear states when the manager itself is destroyed
    // protected virtual void OnDestroy() // Use 'override' if your base Singleton has OnDestroy
    // {
    //    ClearAllSceneStates();
    // }

    // Consider calling ClearAllSceneStates() from GameRunManager.EndGameSession()
    // before unloading GameRoot, to be absolutely sure state is cleared.
}
