    // File: Scripts/Manager/GlobalStatus/StartGameManager.cs
    using UnityEngine;
    using UnityEngine.SceneManagement; // Required for scene management
    using System.Collections;         // Required for Coroutines (IEnumerator)

    /// <summary>
    /// Handles the process of starting a new game session by loading necessary scenes.
    /// 通过加载必要的场景来处理开始新游戏会话的过程。
    /// </summary>
    public class StartGameManager : MonoBehaviour
    {
        // --- Configuration ---
        [Tooltip("Name of the scene containing core game managers (GameRoot).")]
        // [Tooltip("包含核心游戏管理器（GameRoot）的场景名称。")]
        [SerializeField] private string gameRootSceneName = "GameRoot";

        [Tooltip("Name of the first level scene to load.")]
        // [Tooltip("要加载的第一个关卡场景的名称。")]
        [SerializeField] private string firstLevelSceneName = "Level1"; // Make sure this matches your scene file // 确保这与你的场景文件匹配

        [Tooltip("Name of the Main Menu scene.")]
        // [Tooltip("主菜单场景的名称。")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene"; // Make sure this matches your scene file // 确保这与你的场景文件匹配

        [Tooltip("Name/Tag of the GameObject marking the initial player spawn point in the first level.")]
        // [Tooltip("在第一个关卡中标记初始玩家生成点的 GameObject 的名称/标签。")]
        [SerializeField] private string initialSpawnPointName = "InitialSpawnPoint"; // Ensure this object exists in your first level scene // 确保此对象存在于你的第一个关卡场景中

        [Tooltip("Reference to a loading screen UI element (optional).")]
        // [Tooltip("对加载屏幕 UI 元素（可选）的引用。")]
        [SerializeField] private GameObject loadingScreen; // Assign in Inspector if you have one // 如果有，请在 Inspector 中分配

        private bool isLoading = false; // Prevent double clicks // 防止重复点击

        void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.AddListener<GameStartEvent>(HandleGameStartRequest); // Changed method name // 更改了方法名称
                Debug.Log("[StartGameManager] Registered listener for GameStartEvent.");
            }
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
            if (isLoading) return; // Don't start loading if already loading // 如果已在加载，则不开始加载

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

            // --- Set Game Status to Loading ---
            // --- 将游戏状态设置为 Loading ---
            // Do this early so other systems know a transition is happening
            // 尽早执行此操作，以便其他系统知道正在发生转换
            if (GameRunManager.Instance != null)
            {
                 GameRunManager.Instance.ChangeGameStatus(GameStatus.Loading);
            }

            // --- Optional: Show Loading Screen ---
            // --- 可选：显示加载屏幕 ---
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                // yield return new WaitForSeconds(0.5f); // Optional delay // 可选延迟
            }

            // --- 1. Unload the Main menu scene (if loaded) ---
            // --- 1. 卸载主菜单场景（如果已加载）---
            // Check if it's actually loaded before trying to unload
            // 在尝试卸载之前检查它是否实际已加载
            Scene menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
            if (menuScene.isLoaded)
            {
                Debug.Log($"[StartGameManager] Starting to unload scene: {mainMenuSceneName}");
                AsyncOperation unloadMenuOperation = SceneManager.UnloadSceneAsync(mainMenuSceneName);
                if (unloadMenuOperation != null)
                {
                    while (!unloadMenuOperation.isDone)
                    {
                        yield return null; // Wait for the next frame // 等待下一帧
                    }
                    Debug.Log($"[StartGameManager] Scene unloaded: {mainMenuSceneName}");
                } else { Debug.LogWarning($"[StartGameManager] Failed to start unloading {mainMenuSceneName}."); }
            } else { Debug.Log($"[StartGameManager] Main menu scene '{mainMenuSceneName}' not loaded, skipping unload."); }


            // --- 2. Ensure GameRoot scene is loaded (if not already) ---
            // --- 2. 确保 GameRoot 场景已加载（如果尚未加载）---
            // This might be redundant if GameRoot is loaded from Boot, but good safety check
            // 如果 GameRoot 是从 Boot 加载的，这可能是多余的，但是一个好的安全检查
            Scene rootScene = SceneManager.GetSceneByName(gameRootSceneName);
            if (!rootScene.isLoaded)
            {
                Debug.Log($"[StartGameManager] Starting to load scene: {gameRootSceneName} additively");
                AsyncOperation loadRootOperation = SceneManager.LoadSceneAsync(gameRootSceneName, LoadSceneMode.Additive);
                 if (loadRootOperation != null) {
                    while (!loadRootOperation.isDone)
                    {
                        yield return null;
                    }
                    Debug.Log($"[StartGameManager] Scene loaded: {gameRootSceneName}");
                 } else { Debug.LogError($"[StartGameManager] Failed to start loading {gameRootSceneName}!"); }
            } else { Debug.Log($"[StartGameManager] GameRoot scene '{gameRootSceneName}' already loaded."); }


            // --- 3. Load the first Level scene additively ---
            // --- 3. 以 Additive 模式加载第一个关卡场景 ---
            Debug.Log($"[StartGameManager] Starting to load scene: {firstLevelSceneName} additively");
            AsyncOperation loadLevelOperation = SceneManager.LoadSceneAsync(firstLevelSceneName, LoadSceneMode.Additive);
             if (loadLevelOperation == null) {
                 Debug.LogError($"[StartGameManager] Failed to start loading {firstLevelSceneName}!");
                 isLoading = false;
                 if(loadingScreen != null) loadingScreen.SetActive(false);
                 // Maybe return to menu or show error?
                 // 也许返回菜单或显示错误？
                 if(GameRunManager.Instance != null) GameRunManager.Instance.ChangeGameStatus(GameStatus.InMenu); // Example recovery // 示例恢复
                 yield break;
             }
            while (!loadLevelOperation.isDone)
            {
                // Optional: Update loading progress UI here using loadLevelOperation.progress
                // 可选：在此处使用 loadLevelOperation.progress 更新加载进度 UI
                yield return null; // Wait for the next frame // 等待下一帧
            }
            Debug.Log($"[StartGameManager] Scene loaded: {firstLevelSceneName}");

            // --- 4. Set the loaded level scene as the active scene ---
            // --- 4. 将加载的关卡场景设置为活动场景 ---
            // Crucial for lighting, physics, and finding objects correctly
            // 对于正确的光照、物理和查找对象至关重要
            Scene levelScene = SceneManager.GetSceneByName(firstLevelSceneName);
            if (levelScene.IsValid())
            {
                SceneManager.SetActiveScene(levelScene);
                Debug.Log($"[StartGameManager] Active scene set to: {firstLevelSceneName}");
            }
            else
            {
                Debug.LogError($"[StartGameManager] Failed to find loaded scene: {firstLevelSceneName}. Cannot set active scene.");
                isLoading = false;
                if(loadingScreen != null) loadingScreen.SetActive(false);
                 if(GameRunManager.Instance != null) GameRunManager.Instance.ChangeGameStatus(GameStatus.InMenu); // Example recovery // 示例恢复
                yield break; // Exit coroutine // 退出协程
            }

            // --- 5. Reset Player State ---
            // --- 5. 重置玩家状态 ---
            // Wait a frame to ensure objects in the new scene are potentially ready
            // 等待一帧以确保新场景中的对象可能已准备就绪
            yield return null;

            GameObject spawnPointObject = GameObject.Find(initialSpawnPointName); // Find spawn point by name // 按名称查找生成点
            if (spawnPointObject != null)
            {
                if (Player.Instance != null)
                {
                    Player.Instance.ResetPlayerState(spawnPointObject.transform.position);
                }
                else
                {
                    Debug.LogError("[StartGameManager] Player.Instance is null! Cannot reset player state.");
                }
            }
            else
            {
                Debug.LogError($"[StartGameManager] Initial spawn point GameObject named '{initialSpawnPointName}' not found in scene '{firstLevelSceneName}'! Cannot reset player position.");
                // Consider spawning player at Vector3.zero as a fallback?
                // 考虑在 Vector3.zero 生成玩家作为后备方案？
                // if (Player.Instance != null) Player.Instance.ResetPlayerState(Vector3.zero);
            }

            // --- 6. Transition to Playing State ---
            // --- 6. 转换到 Playing 状态 ---
            if (GameRunManager.Instance != null)
            {
                 // Now that loading and setup are done, set the game to Playing
                 // 现在加载和设置已完成，将游戏设置为 Playing
                 GameRunManager.Instance.ChangeGameStatus(GameStatus.Playing);
                 Debug.Log("[StartGameManager] Game Status set to Playing.");
            }


            // --- Optional: Hide Loading Screen ---
            // --- 可选：隐藏加载屏幕 ---
            if (loadingScreen != null)
            {
                // Optional: Add a small delay or fade-out animation here
                // 可选：在此处添加短暂延迟或淡出动画
                // yield return new WaitForSeconds(0.5f);
                loadingScreen.SetActive(false);
            }

            isLoading = false;
            Debug.Log("[StartGameManager] Game loading sequence complete.");
        }
    }
    