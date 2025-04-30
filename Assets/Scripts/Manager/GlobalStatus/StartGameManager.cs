// File: Scripts/Manager/GlobalStatus/StartGameManager.cs (修改后)
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
// using YourEventsNamespace; // 如果事件在命名空间中

/// <summary>
/// Handles the process of starting a new game session by loading necessary scenes.
/// 通过加载必要的场景来处理开始新游戏会话的过程。
/// Now triggers GameReadyToPlayEvent after loading is complete and status is Playing.
/// 现在在加载完成且状态为 Playing 后触发 GameReadyToPlayEvent。
/// </summary>
public class StartGameManager : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("Name of the scene containing core game managers (GameRoot).")]
    [SerializeField] private string gameRootSceneName = "GameRoot";

    [Tooltip("Name of the first level scene to load.")]
    [SerializeField] private string firstLevelSceneName = "Level1";

    [Tooltip("Name of the Main Menu scene.")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Tooltip("Name/Tag of the GameObject marking the initial player spawn point in the first level.")]
    [SerializeField] private string initialSpawnPointName = "InitialSpawnPoint";

    [Tooltip("Reference to a loading screen UI element (optional).")]
    [SerializeField] private GameObject loadingScreen;

    private bool isLoading = false; // Prevent double clicks

    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStartEvent>(HandleGameStartRequest);
            Debug.Log("[StartGameManager] Registered listener for GameStartEvent.");
        } else { Debug.LogError("[StartGameManager] EventManager not found on Enable!"); }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStartEvent>(HandleGameStartRequest);
            Debug.Log("[StartGameManager] Unregistered listener for GameStartEvent.");
        }
    }

    /// <summary>
    /// Handles the GameStartEvent triggered by the main menu or automatic restart.
    /// 处理由主菜单或自动重新开始触发的 GameStartEvent。
    /// </summary>
    private void HandleGameStartRequest(GameStartEvent eventData)
    {
        Debug.Log($"[StartGameManager] GameStartEvent received. isLoading: {isLoading}");
        if (isLoading) return;

        Debug.Log("[StartGameManager] Beginning loading sequence...");
        StartCoroutine(LoadGameSequence());
    }

    /// <summary>
    /// Coroutine to handle the asynchronous loading of game scenes and player reset.
    /// 处理游戏场景异步加载和玩家重置的协程。
    /// </summary>
    private IEnumerator LoadGameSequence()
    {
        isLoading = true;

        // --- 设置游戏状态为 Loading ---
        // --- Set Game Status to Loading ---
        if (GameRunManager.Instance != null)
        {
             GameRunManager.Instance.ChangeGameStatus(GameStatus.Loading);
        } else { Debug.LogError("[StartGameManager] GameRunManager not found at start of LoadGameSequence!"); } // Add error check

        // --- 显示加载屏幕 (Optional: Show Loading Screen) ---
        if (loadingScreen != null) loadingScreen.SetActive(true);

        // --- 1. 卸载主菜单 (Unload Main Menu) ---
        Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menuScene.isLoaded)
        {
            Debug.Log($"[StartGameManager] Starting to unload scene: {mainMenuSceneName}");
            AsyncOperation unloadMenuOperation = SceneManager.UnloadSceneAsync(mainMenuSceneName);
            if (unloadMenuOperation != null) while (!unloadMenuOperation.isDone) yield return null;
            else Debug.LogWarning($"[StartGameManager] Failed to start unloading {mainMenuSceneName}.");
            Debug.Log($"[StartGameManager] Scene unloaded: {mainMenuSceneName}");
        }

        // --- 2. 确保 GameRoot 加载 (Ensure GameRoot Loaded) ---
        Scene rootScene = SceneManager.GetSceneByName(gameRootSceneName);
        if (!rootScene.isLoaded)
        {
            Debug.Log($"[StartGameManager] Starting to load scene: {gameRootSceneName} additively");
            AsyncOperation loadRootOperation = SceneManager.LoadSceneAsync(gameRootSceneName, LoadSceneMode.Additive);
             if (loadRootOperation != null) while (!loadRootOperation.isDone) yield return null;
             else Debug.LogError($"[StartGameManager] Failed to start loading {gameRootSceneName}!");
            Debug.Log($"[StartGameManager] Scene loaded: {gameRootSceneName}");
        }

        // --- 3. 加载第一个关卡 (Load First Level) ---
        Debug.Log($"[StartGameManager] Starting to load scene: {firstLevelSceneName} additively");
        AsyncOperation loadLevelOperation = SceneManager.LoadSceneAsync(firstLevelSceneName, LoadSceneMode.Additive);
         if (loadLevelOperation == null) {
             Debug.LogError($"[StartGameManager] Failed to start loading {firstLevelSceneName}!");
             isLoading = false;
             if(loadingScreen != null) loadingScreen.SetActive(false);
             GameRunManager.Instance?.ChangeGameStatus(GameStatus.InMenu); // 尝试恢复 (Try to recover)
             yield break;
         }
        while (!loadLevelOperation.isDone) yield return null;
        Debug.Log($"[StartGameManager] Scene loaded: {firstLevelSceneName}");

        // --- 4. 设置活动场景 (Set Active Scene) ---
        Scene levelScene = SceneManager.GetSceneByName(firstLevelSceneName);
        if (levelScene.IsValid())
        {
            SceneManager.SetActiveScene(levelScene);
            Debug.Log($"[StartGameManager] Active scene set to: {firstLevelSceneName}");
        }
        else { /* Error Handling */ yield break; }

        // --- 5. 重置玩家状态 (Reset Player State) ---
        yield return null; // 等待一帧确保对象初始化 (Wait a frame for object initialization)
        GameObject spawnPointObject = GameObject.Find(initialSpawnPointName);
        if (spawnPointObject != null)
        {
            Player.Instance?.ResetPlayerState(spawnPointObject.transform.position);
            if (Player.Instance == null) Debug.LogError("[StartGameManager] Player.Instance is null! Cannot reset player state.");
        }
        else { Debug.LogError($"[StartGameManager] Initial spawn point '{initialSpawnPointName}' not found!"); }

        // --- 6. 转换到 Playing 状态 ---
        // --- 6. Transition to Playing State ---
        GameRunManager.Instance?.ChangeGameStatus(GameStatus.Playing); // 确保 GameRunManager 存在 (Ensure GameRunManager exists)
        Debug.Log("[StartGameManager] Game Status set to Playing.");

        // --- 7. 触发游戏准备就绪事件 ---
        // --- 7. Trigger Game Ready To Play Event ---
        // 在所有加载和设置完成后触发
        // Trigger after all loading and setup is complete
        Debug.Log("[StartGameManager] Triggering GameReadyToPlayEvent.");
        EventManager.Instance?.TriggerEvent(new GameReadyToPlayEvent());
        if (EventManager.Instance == null) Debug.LogError("[StartGameManager] EventManager not found! Cannot trigger GameReadyToPlayEvent.");

        // --- 8. 标记首次加载完成 (Mark Initial Load Complete) ---
        // 在触发事件后标记，确保依赖此事件的逻辑先执行
        // Mark after triggering the event, ensuring logic depending on it runs first
        GameRunManager.Instance?.MarkInitialLoadComplete();


        // --- 隐藏加载屏幕 (Optional: Hide Loading Screen) ---
        if (loadingScreen != null) loadingScreen.SetActive(false);

        isLoading = false;
        Debug.Log("[StartGameManager] Game loading sequence complete.");
    }
}
