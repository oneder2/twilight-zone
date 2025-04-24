// File: Scripts/Manager/GlobalStatus/EndGameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles returning to the main menu, including unloading game scenes and potentially triggering an automatic restart.
/// 处理返回主菜单，包括卸载游戏场景和可能触发自动重新开始。
/// Attach this to a GameObject in your PauseMenu or a persistent UI manager.
/// 将此附加到 PauseMenu 中的 GameObject 或持久性 UI 管理器上。
/// </summary>
public class EndGameManager : MonoBehaviour
{
    // --- Configuration ---
    [Tooltip("Name of the scene containing core game managers (GameRoot).")]
    // [Tooltip("包含核心游戏管理器（GameRoot）的场景名称。")]
    [SerializeField] private string gameRootSceneName = "GameRoot";

    [Tooltip("Name of the Boot scene (containing persistent managers).")]
    // [Tooltip("Boot 场景（包含持久性管理器）的名称。")]
    [SerializeField] private string bootSceneName = "Boot";

    [Tooltip("Name of the Main Menu scene.")]
    // [Tooltip("主菜单场景的名称。")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Tooltip("Reference to a loading screen UI element (optional).")]
    // [Tooltip("对加载屏幕 UI 元素（可选）的引用。")]
    [SerializeField] private GameObject loadingScreen;

    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameEndEvent>(HandleGameEndRequest);
            Debug.Log("[EndGameManager] Registered listener for GameEndEvent.");
        }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameEndEvent>(HandleGameEndRequest);
            Debug.Log("[EndGameManager] Unregistered listener for GameEndEvent.");
        }
    }

    /// <summary>
    /// Handles the GameEndEvent triggered by GameOverUI or pause menu.
    /// 处理由 GameOverUI 或暂停菜单触发的 GameEndEvent。
    /// </summary>
    private void HandleGameEndRequest(GameEndEvent eventData)
    {
        if (GameRunManager.Instance == null) { Debug.LogError("[EndGameManager] GameRunManager not found!"); return; }
        if (GameRunManager.Instance.IsSceneLoadingOrUnloading)
        {
            Debug.LogWarning("[EndGameManager] Received GameEndEvent while another scene operation is in progress. Ignoring.");
            return; // Exit if already loading/unloading // 如果已在加载/卸载，则退出
        }

        Debug.Log("[EndGameManager] GameEndEvent received. Beginning unload/load menu sequence...");
        StartCoroutine(UnloadAndLoadMenuSequence());
    }

    /// <summary>
    /// Coroutine to handle unloading game scenes, loading the main menu, and checking for automatic restart.
    /// 处理卸载游戏场景、加载主菜单并检查自动重新开始的协程。
    /// </summary>
    private IEnumerator UnloadAndLoadMenuSequence()
    {
        // --- Set Loading Lock ---
        // --- 设置加载锁 ---
        if (GameRunManager.Instance != null) GameRunManager.Instance.SetLoadingStatus(true);
        else { Debug.LogError("[EndGameManager] Cannot set loading lock, GameRunManager is null!"); yield break; } // Critical error // 严重错误

        // Use try-finally to ensure the loading status is reset
        // 使用 try-finally 确保加载状态被重置
        try
        {
            Debug.Log("[EndGameManager Coroutine] Started.");

            // --- Optional: Show Loading Screen / Fade Out ---
            // --- 可选：显示加载屏幕/淡出 ---
            if (loadingScreen != null) loadingScreen.SetActive(true);
            // Consider adding a fade out here using TransitionManager or similar // 考虑在此处使用 TransitionManager 或类似方式添加淡出

            // --- 1. Notify GameRunManager to end the session *before* unloading ---
            // --- 1. 在卸载*之前*通知 GameRunManager 结束会话 ---
            // This sets state to InMenu, plays menu music, clears session state etc.
            // 这会将状态设置为 InMenu，播放菜单音乐，清除会话状态等。
            if (GameRunManager.Instance != null) GameRunManager.Instance.EndGameSession();
            else Debug.LogWarning("[EndGameManager Coroutine] GameRunManager instance not found during EndGameSession call.");

            // --- 2. Get the current active scene (the level scene) ---
            // --- 2. 获取当前活动场景（关卡场景）---
            Scene currentLevelScene = SceneManager.GetActiveScene();
            string currentLevelName = currentLevelScene.name;
            Debug.Log($"[EndGameManager Coroutine] Current level scene to unload: {currentLevelName}");

            // --- 3. Unload the game level scene ---
            // --- 3. 卸载游戏关卡场景 ---
            // Ensure it's valid and not a core scene before unloading
            // 在卸载之前确保它是有效的并且不是核心场景
            if (currentLevelScene.IsValid() && currentLevelScene.isLoaded && currentLevelName != bootSceneName && currentLevelName != gameRootSceneName && currentLevelName != mainMenuSceneName)
            {
                Debug.Log($"[EndGameManager Coroutine] Starting to unload scene: {currentLevelName}");
                AsyncOperation unloadLevelOp = SceneManager.UnloadSceneAsync(currentLevelName);
                if (unloadLevelOp != null)
                {
                    while (!unloadLevelOp.isDone)
                    {
                         // Debug.Log($"[EndGameManager Coroutine] Unloading level progress: {unloadLevelOp.progress}"); // Verbose logging // 冗余日志
                        yield return null;
                    }
                    Debug.Log($"[EndGameManager Coroutine] Scene unloaded: {currentLevelName}");
                } else { Debug.LogWarning($"[EndGameManager Coroutine] Failed to start unloading level scene '{currentLevelName}'."); }
            }
            else Debug.LogWarning($"[EndGameManager Coroutine] Skipping unload for scene '{currentLevelName}' (Invalid, not loaded, or core scene).");


            // --- 4. Unload the GameRoot scene (if it's separate from Boot and needs unloading) ---
            // --- 4. 卸载 GameRoot 场景（如果它与 Boot 分开并且需要卸载）---
            Scene gameRoot = SceneManager.GetSceneByName(gameRootSceneName);
            if (!string.IsNullOrEmpty(gameRootSceneName) && gameRootSceneName != bootSceneName && gameRoot.isLoaded)
            {
                Debug.Log($"[EndGameManager Coroutine] Starting to unload scene: {gameRootSceneName}");
                AsyncOperation unloadRootOp = SceneManager.UnloadSceneAsync(gameRootSceneName);
                 if (unloadRootOp != null)
                 {
                    while (!unloadRootOp.isDone)
                    {
                         // Debug.Log($"[EndGameManager Coroutine] Unloading GameRoot progress: {unloadRootOp.progress}"); // Verbose logging // 冗余日志
                        yield return null;
                    }
                    Debug.Log($"[EndGameManager Coroutine] Scene unloaded: {gameRootSceneName}");
                 } else { Debug.LogWarning($"[EndGameManager Coroutine] Failed to start unloading GameRoot scene '{gameRootSceneName}'."); }
            } else { Debug.Log($"[EndGameManager Coroutine] Skipping unload for GameRoot scene '{gameRootSceneName}' (Not loaded or is Boot)."); }


            // --- 5. Load the Main Menu scene additively ---
            // --- 5. 以 Additive 模式加载主菜单场景 ---
            // Check if it's already loaded first (might happen in editor or complex flows)
            // 首先检查它是否已加载（可能在编辑器或复杂流程中发生）
            Scene mainMenuScene = SceneManager.GetSceneByName(mainMenuSceneName);
            if (!mainMenuScene.isLoaded)
            {
                Debug.Log($"[EndGameManager Coroutine] Starting to load scene: {mainMenuSceneName} additively");
                AsyncOperation loadMenuOp = SceneManager.LoadSceneAsync(mainMenuSceneName, LoadSceneMode.Additive);
                 if (loadMenuOp != null)
                 {
                    while (!loadMenuOp.isDone)
                    {
                         // Debug.Log($"[EndGameManager Coroutine] Loading MainMenu progress: {loadMenuOp.progress}"); // Verbose logging // 冗余日志
                        yield return null;
                    }
                    Debug.Log($"[EndGameManager Coroutine] Scene loaded additively: {mainMenuSceneName}");
                    mainMenuScene = SceneManager.GetSceneByName(mainMenuSceneName); // Re-get scene reference after load // 加载后重新获取场景引用
                 } else { Debug.LogError($"[EndGameManager Coroutine] Failed to start loading MainMenu scene '{mainMenuSceneName}'!"); }
            } else { Debug.Log($"[EndGameManager Coroutine] MainMenu scene '{mainMenuSceneName}' already loaded."); }


            // --- 6. Set Main Menu as Active Scene ---
            // --- 6. 将主菜单设置为活动场景 ---
             if (mainMenuScene.IsValid() && mainMenuScene.isLoaded) {
                  // Wait a frame before setting active scene to allow initialization? (Sometimes helps)
                  // 在设置活动场景之前等待一帧以允许初始化？（有时有帮助）
                  yield return null;
                  SceneManager.SetActiveScene(mainMenuScene);
                  Debug.Log($"[EndGameManager Coroutine] Set Main Menu scene '{mainMenuSceneName}' as active.");
             } else {
                  Debug.LogError($"[EndGameManager Coroutine] Could not find or set loaded Main Menu scene '{mainMenuSceneName}' as active.");
             }

            // --- Optional: Hide Loading Screen ---
            // --- 可选：隐藏加载屏幕 ---
            if (loadingScreen != null) loadingScreen.SetActive(false);

            // --- 7. Check for Automatic Restart ---
            // --- 7. 检查是否需要自动重新开始 ---
            // This check happens AFTER all scene ops are done and menu is active
            // 此检查在所有场景操作完成且菜单处于活动状态之后进行
            if (GameRunManager.InitiateRestartFlow)
            {
                Debug.Log("[EndGameManager Coroutine] InitiateRestartFlow flag is true. Triggering automatic game start...");
                GameRunManager.InitiateRestartFlow = false; // Reset the flag // 重置标志
                if (EventManager.Instance != null) EventManager.Instance.TriggerEvent(new GameStartEvent());
                else Debug.LogError("[EndGameManager Coroutine] EventManager instance not found! Cannot trigger automatic GameStartEvent.");
            }
            else Debug.Log("[EndGameManager Coroutine] InitiateRestartFlow flag is false. Staying in Main Menu.");

            Debug.Log("[EndGameManager Coroutine] Unload/Load Menu sequence complete.");
        }
        // catch (System.Exception ex) // Catch potential errors during the sequence // 捕获序列期间的潜在错误
        // {
        //      Debug.LogError($"[EndGameManager Coroutine] Exception occurred: {ex.Message}\n{ex.StackTrace}");
        // }
        finally // Ensure the lock is released // 确保锁被释放
        {
            // --- Release Loading Lock ---
            // --- 释放加载锁 ---
            if (GameRunManager.Instance != null) GameRunManager.Instance.SetLoadingStatus(false);
             else { Debug.LogError("[EndGameManager] Cannot release loading lock, GameRunManager is null!"); }
            Debug.Log("[EndGameManager Coroutine] Loading lock released.");
        }
    }
}
