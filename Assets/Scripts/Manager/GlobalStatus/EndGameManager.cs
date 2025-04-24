using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;         // Required for Coroutines (IEnumerator)

/// <summary>
/// Example script demonstrating how to return to the main menu.
/// Attach this to a GameObject in your PauseMenu (which should likely be part of your Game UI Canvas in GameRoot).
/// </summary>
public class EndGameManager : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("Name of the scene containing core game managers (GameRoot).")]
    [SerializeField] private string gameRootSceneName = "GameRoot";

    [Tooltip("Name of the Boot scene.")]
    [SerializeField] private string bootSceneName = "Boot"; // Make sure this matches your scene file

    [Tooltip("Name of the Main Menu scene.")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene"; // Make sure this matches your scene file

    [Tooltip("Reference to a loading screen UI element (optional).")]
    [SerializeField] private GameObject loadingScreen; // Assign in Inspector if you have one

    private bool isUnloading = false; // Prevent double clicks

    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameEndEvent>(GoToMainMenu);
            Debug.Log("EndGameManager registered on EndStartEvent.");
        }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameEndEvent>(GoToMainMenu);
            Debug.Log("EndGameManager unregistered on EndStartEvent.");
        }
    }

    /// <summary>
    /// Public method to be called by the 'Return to Menu' button's OnClick() event.
    /// </summary>
    private void GoToMainMenu(GameEndEvent eventData)
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
    Debug.Log("GoToMainMenu called. Beginning unloading sequence...");

    if (loadingScreen != null) loadingScreen.SetActive(true);

    // 1. 通知 GameRunManager 结束会话
    if (GameRunManager.Instance != null)
    {
        GameRunManager.Instance.EndGameSession();
    }
    else
    {
        Debug.LogWarning("GameRunManager instance not found. Cannot properly end game session.");
    }

    // 2. 获取当前游戏关卡场景
    Scene currentLevelScene = SceneManager.GetActiveScene();
    string currentLevelName = currentLevelScene.name;
    Debug.Log($"Current level scene to unload: {currentLevelName}");

    // --- 先加载主菜单 (Additive) ---
    // 这样可以确保在卸载旧场景时，至少有一个场景是加载的
    Debug.Log($"Starting to load scene: {mainMenuSceneName} additively");
    AsyncOperation loadMenuOperation = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
    if (loadMenuOperation == null) {
         Debug.LogError("Failed to start loading Main Menu scene additively.");
         isUnloading = false;
         if (loadingScreen != null) loadingScreen.SetActive(false);
         yield break;
    }
    while (!loadMenuOperation.isDone)
    {
        yield return null;
    }
    Debug.Log($"Scene loaded additively: {mainMenuSceneName}");
    // 可选：将主菜单设置为 Active Scene，但这可能需要稍后处理，
    // 因为现在可能有多个场景加载着。通常在卸载完其他场景后再设置。


    // --- 卸载游戏关卡场景 ---
    AsyncOperation unloadLevelOperation = null;
    if (currentLevelScene.IsValid() && currentLevelScene.isLoaded && currentLevelName != bootSceneName && currentLevelName != gameRootSceneName /*避免卸载GameRoot或Boot*/)
    {
        Debug.Log($"Starting to unload scene: {currentLevelName}");
        unloadLevelOperation = SceneManager.UnloadSceneAsync(currentLevelName);
        while (unloadLevelOperation != null && !unloadLevelOperation.isDone)
        {
            yield return null;
        }
        Debug.Log($"Scene unloaded: {currentLevelName}");
    }
    else
    {
        Debug.LogWarning($"Skipping unload for scene '{currentLevelName}' as it might be invalid, not loaded, or a core scene.");
    }

    // --- 卸载 GameRoot 场景 (如果它不是 Boot 场景且需要卸载) ---
    // **再次确认 gameRootSceneName 不是你的持久化 Boot 场景名**
    AsyncOperation unloadRootOperation = null;
    if (!string.IsNullOrEmpty(gameRootSceneName) && gameRootSceneName != bootSceneName && SceneManager.GetSceneByName(gameRootSceneName).isLoaded)
    {
        Debug.Log($"Starting to unload scene: {gameRootSceneName}");
        unloadRootOperation = SceneManager.UnloadSceneAsync(gameRootSceneName);
        while (unloadRootOperation != null && !unloadRootOperation.isDone)
        {
            yield return null;
        }
        Debug.Log($"Scene unloaded: {gameRootSceneName}");
    }


    // --- 设置主菜单为 Active Scene ---
    // 此时应该只剩下 Boot 和 MainMenu 了
     Scene mainMenu = SceneManager.GetSceneByName(mainMenuSceneName);
     if (mainMenu.IsValid()) {
          SceneManager.SetActiveScene(mainMenu);
          Debug.Log($"Set Main Menu scene '{mainMenuSceneName}' as active.");
     } else {
          Debug.LogError($"Could not find loaded Main Menu scene '{mainMenuSceneName}' to set active.");
     }


    if (loadingScreen != null) loadingScreen.SetActive(false);
    isUnloading = false;
    Debug.Log("Unloading sequence complete. Main menu should be active.");
}
}
