using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;         // Required for Coroutines (IEnumerator)

/// <summary>
/// Example script demonstrating how to start the game session.
/// Attach this to a GameObject in your MainMenu scene.
/// </summary>
public class StartGameManager : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("Name of the scene containing core game managers (GameRoot).")]
    [SerializeField] private string gameRootSceneName = "GameRoot";

    [Tooltip("Name of the first level scene to load.")] 
    [SerializeField] private string firstLevelSceneName = "Level1"; // Make sure this matches your scene file

    [Tooltip("Name of the Main Menu scene.")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene"; // Make sure this matches your scene file

    [Tooltip("Reference to a loading screen UI element (optional).")]
    [SerializeField] private GameObject loadingScreen; // Assign in Inspector if you have one

    private bool isLoading = false; // Prevent double clicks

    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStartEvent>(StartNewGame);
            Debug.Log("StartGameManager registered on GameStartEvent.");
        }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStartEvent>(StartNewGame);
            Debug.Log("StartGameManager unregistered on GameStartEvent.");
        }
    }

    /// <summary>
    /// Public method to be called by the 'Start Game' button's OnClick() event.
    /// </summary>
    void StartNewGame(GameStartEvent eventData)
    {
        Debug.Log($"entered StartNewGame method, if is loading:{isLoading}");
        if (isLoading) return; // Don't start loading if already loading

        Debug.Log("StartNewGame called. Beginning loading sequence...");
        StartCoroutine(LoadGameSequence());
    }

    /// <summary>
    /// Coroutine to handle the asynchronous loading of game scenes.
    /// 
    /// Unload the Main menu scene, and load:
    /// GameRoot: the global variable session.
    /// Classroom3 session: arranged to be the initial session of the game.
    /// </summary>
    private IEnumerator LoadGameSequence()
    {
        isLoading = true;

        GameRunManager.Instance.StartGameSession();

        // --- Optional: Show Loading Screen ---
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            // Optional: Add a small delay or fade-in animation here
            yield return new WaitForSeconds(0.5f);
        }

        // --- 0. Unload the Main menu page ---

        Debug.Log($"Starting to unloadload scene: {mainMenuSceneName}");
        AsyncOperation unloadMenuOperation = SceneManager.UnloadSceneAsync(mainMenuSceneName);
        while (!unloadMenuOperation.isDone)
        {
            // Optional: Update loading progress UI here using loadRootOperation.progress
            yield return null; // Wait for the next frame
        }
        Debug.Log($"Scene unloaded: {mainMenuSceneName}");

        // --- 1. Start loading GameRoot scene additively ---
        Debug.Log($"Starting to load scene: {gameRootSceneName}");
        AsyncOperation loadRootOperation = SceneManager.LoadSceneAsync(gameRootSceneName, LoadSceneMode.Additive);
        while (!loadRootOperation.isDone)
        {
            // Optional: Update loading progress UI here using loadRootOperation.progress
            yield return null; // Wait for the next frame
        }
        Debug.Log($"Scene loaded: {gameRootSceneName}");

        // --- 2. Start loading the first Level scene additively ---
        Debug.Log($"Starting to load scene: {firstLevelSceneName}");
        AsyncOperation loadLevelOperation = SceneManager.LoadSceneAsync(firstLevelSceneName, LoadSceneMode.Additive);
        while (!loadLevelOperation.isDone)
        {
            // Optional: Update loading progress UI here using loadLevelOperation.progress
            yield return null; // Wait for the next frame
        }
        Debug.Log($"Scene loaded: {firstLevelSceneName}");

        // --- 3. Set the loaded level scene as the active scene ---
        // It's crucial to do this *after* the level scene has finished loading.
        Scene levelScene = SceneManager.GetSceneByName(firstLevelSceneName);
        if (levelScene.IsValid())
        {
            SceneManager.SetActiveScene(levelScene);
            Debug.Log($"Active scene set to: {firstLevelSceneName}");
        }
        else
        {
            Debug.LogError($"Failed to find loaded scene: {firstLevelSceneName}. Cannot set active scene.");
            // Handle error appropriately - maybe return to menu?
            isLoading = false;
            yield break; // Exit coroutine
        }
        Debug.Log("————————————————————————————————————————————————————————————");

        // --- Game is ready, GameRunManager.Start() will execute soon ---
        // The GameRunManager in GameRoot will handle setting the game state and triggering events.

        // // --- Optional: Hide Loading Screen ---
        if (loadingScreen != null)
        {
            // Optional: Add a small delay or fade-out animation here
            yield return new WaitForSeconds(0.5f);
            loadingScreen.SetActive(false);
        }

        isLoading = false;
        Debug.Log("Game loading sequence complete.");
    }
}