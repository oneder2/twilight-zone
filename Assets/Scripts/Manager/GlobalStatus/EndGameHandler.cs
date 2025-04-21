using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;         // Required for Coroutines (IEnumerator)

/// <summary>
/// Example script demonstrating how to return to the main menu.
/// Attach this to a GameObject in your PauseMenu (which should likely be part of your Game UI Canvas in GameRoot).
/// </summary>
public class ReturnToMenuHandler : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("Name of the scene containing core game managers (GameRoot).")]
    [SerializeField] private string gameRootSceneName = "GameRoot";

    [Tooltip("Name of the Main Menu scene.")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene"; // Make sure this matches your scene file

    [Tooltip("Reference to a loading screen UI element (optional).")]
    [SerializeField] private GameObject loadingScreen; // Assign in Inspector if you have one

    private bool isUnloading = false; // Prevent double clicks

    /// <summary>
    /// Public method to be called by the 'Return to Menu' button's OnClick() event.
    /// </summary>
    public void GoToMainMenu()
    {
        if (isUnloading) return;

        Debug.Log("GoToMainMenu called. Beginning unloading sequence...");
        StartCoroutine(UnloadGameSequence());
    }

    /// <summary>
    /// Coroutine to handle unloading game scenes and loading the main menu.
    /// </summary>
    private IEnumerator UnloadGameSequence()
    {
        isUnloading = true;

        // --- Optional: Show Loading Screen / Fade Out ---
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            // yield return new WaitForSeconds(0.5f); // Optional delay/fade
        }

        // --- 1. Notify GameRunManager to end the session *before* unloading ---
        // This allows GameRunManager to set state, trigger events, and do pre-unload cleanup.
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.EndGameSession();
        }
        else
        {
            Debug.LogWarning("GameRunManager instance not found. Cannot properly end game session.");
        }

        // --- 2. Get the current active scene (the level scene) ---
        Scene currentLevelScene = SceneManager.GetActiveScene();
        string currentLevelName = currentLevelScene.name;
        Debug.Log($"Current level scene to unload: {currentLevelName}");

        // --- 6. Load the Main Menu scene ---
        // Using Single mode automatically cleans up any remaining scenes (though we unloaded manually above).
        Debug.Log($"Starting to load scene: {mainMenuSceneName}");
        AsyncOperation loadMenuOperation = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Single);
        while (!loadMenuOperation.isDone)
        {
            // Optional: Update loading progress UI here
            yield return null;
        }

        // --- Loading screen might be hidden automatically by the new scene, or handle it here ---
        // if (loadingScreen != null) loadingScreen.SetActive(false);

        isUnloading = false; // Allow loading again if needed (though usually menu handles this)
        Debug.Log("Unloading sequence complete. Main menu loaded.");

        // --- 3. Start unloading GameRoot ---
        Debug.Log($"Starting to unload scene: {gameRootSceneName}");
        AsyncOperation unloadRootOperation = SceneManager.UnloadSceneAsync(gameRootSceneName);
        // We might not need to wait for this one individually if we wait for both below

        // --- 4. Start unloading the current level scene ---
        Debug.Log($"Starting to unload scene: {currentLevelName}");
        AsyncOperation unloadLevelOperation = null;
        if (currentLevelScene.IsValid() && currentLevelScene.isLoaded && currentLevelName != gameRootSceneName) // Check if it's a valid, loaded, different scene
        {
             unloadLevelOperation = SceneManager.UnloadSceneAsync(currentLevelName);
        }
        else
        {
            Debug.LogWarning($"Skipping unload for scene '{currentLevelName}' as it might be invalid, not loaded, or same as GameRoot.");
        }


        // --- 5. Wait for both unload operations to complete ---
        while (unloadRootOperation != null && !unloadRootOperation.isDone)
        {
             yield return null;
        }
         Debug.Log($"Scene unloaded: {gameRootSceneName}");

        while (unloadLevelOperation != null && !unloadLevelOperation.isDone)
        {
             yield return null;
        }
        if (unloadLevelOperation != null) Debug.Log($"Scene unloaded: {currentLevelName}");
    }
}
