// File: Scripts/Interactable/Teleport/TransitionManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages scene transitions between levels within the game session.
/// Handles additive loading/unloading, fade effects, and coordinates session state saving/loading.
/// Assumes it resides in the persistent GameRoot or Boot scene.
/// 管理游戏会话中关卡之间的场景转换。
/// 处理附加加载/卸载、淡入淡出效果，并协调会话状态的保存/加载。
/// 假设它驻留在持久性 GameRoot 或 Boot 场景中。
/// </summary>
public class TransitionManager : Singleton<TransitionManager> // Assuming Singleton base class / 假设有 Singleton 基类
{
    [Header("Fade Effect Settings / 淡入淡出效果设置")]
    [Tooltip("CanvasGroup used for the fade effect.\n用于淡入淡出效果的 CanvasGroup。")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of the fade in/out animation in seconds.\n淡入/淡出动画的持续时间（秒）。")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Scene Configuration / 场景配置")]
    [Tooltip("Name of the scene containing core game managers (this manager). Used for safety checks.\n包含核心游戏管理器（此管理器）的场景名称。用于安全检查。")]
    [SerializeField] private string gameRootSceneName = "GameRoot"; // Or "Boot" / 或 "Boot"

    /// <summary>
    /// Gets whether a fade transition is currently active.
    /// 获取淡入淡出转换当前是否处于活动状态。
    /// </summary>
    public bool IsTransitioning { get; private set; } = false;

    // Ensure fade canvas starts correctly / 确保淡入淡出画布正确启动
    protected override void Awake()
    {
        base.Awake(); // Handle Singleton logic / 处理 Singleton 逻辑
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[TransitionManager] Fade Canvas Group is not assigned!");
            return;
        }
        // Initialize fade state only if this instance survives / 仅当此实例在 Singleton 检查中存活下来时才初始化淡入淡出状态
        if (Instance == this)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false; // Allow clicks through when invisible / 不可见时允许点击穿透
        }
    }

    // --- Event Subscription for Event-Driven Transitions / 事件驱动转换的事件订阅 ---
    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
             EventManager.Instance.AddListener<TransitionRequestedEvent>(HandleTransitionRequest);
             // Debug.Log($"[TransitionManager OnEnable] Subscribed HandleTransitionRequest (Instance ID: {GetInstanceID()})");
        } else { Debug.LogError("[TransitionManager OnEnable] EventManager not found!"); }
    }
    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<TransitionRequestedEvent>(HandleTransitionRequest);
             // Debug.Log($"[TransitionManager OnDisable] Unsubscribed HandleTransitionRequest (Instance ID: {GetInstanceID()})");
        } else { Debug.LogWarning("[TransitionManager OnDisable] EventManager not found during cleanup."); }
    }
    // Optional: OnDestroy logging / 可选：OnDestroy 日志记录

    /// <summary>
    /// Handles the TransitionRequestedEvent.
    /// 处理 TransitionRequestedEvent。
    /// </summary>
    private void HandleTransitionRequest(TransitionRequestedEvent eventData)
    {
        // Debug.Log($"[TransitionManager HandleTransitionRequest] Event received by Instance ID: {GetInstanceID()}. Requesting teleport to '{eventData?.TargetSceneName ?? "NULL"}', TargetID '{eventData?.TargetTeleporterID ?? "NULL"}'");
        if (eventData == null) return;
        InitiateTeleport(eventData.TargetSceneName, eventData.TargetTeleporterID);
    }
    // --- End Event Subscription / 结束事件订阅 ---


    /// <summary>
    /// Public method to initiate the teleport sequence. Can be called directly or via event handler.
    /// 启动传送序列的公共方法。可以直接调用或通过事件处理程序调用。
    /// </summary>
    /// <param name="toSceneName">The name of the target scene to load. / 要加载的目标场景的名称。</param>
    /// <param name="targetTeleporterID">The ID of the teleporter to spawn at in the target scene. / 目标场景中要生成的传送器的 ID。</param>
    public void InitiateTeleport(string toSceneName, string targetTeleporterID)
    {
         // Prevent starting a new transition if one is already running / 如果转换已在运行，则阻止启动新的转换
         if (IsTransitioning)
         {
             Debug.LogWarning($"[TransitionManager] Teleport requested while already transitioning. Ignoring request.");
             return;
         }

         // Get the scene to unload / 获取要卸载的场景
         Scene sceneToUnload = SceneManager.GetActiveScene();

         // Validate inputs / 验证输入
         if (string.IsNullOrEmpty(toSceneName)) { Debug.LogError($"[TransitionManager] Teleport requested with an empty target scene name!"); return; }
         // Prevent unloading the core manager scene / 阻止卸载核心管理器场景
         if (sceneToUnload.name == gameRootSceneName || sceneToUnload.name == "Boot") { Debug.LogError($"[TransitionManager] Attempting to start transition FROM the core scene ('{sceneToUnload.name}')? Aborting."); return; }

         // Debug.Log($"[TransitionManager] Starting teleport coroutine. From: '{sceneToUnload.name}' To: '{toSceneName}' Target ID: '{targetTeleporterID}'");
         StartCoroutine(TransformToScene(sceneToUnload, toSceneName, targetTeleporterID));
    }

    /// <summary>
    /// Coroutine that handles the actual scene loading, unloading, fading, and state management calls.
    /// 处理实际场景加载、卸载、淡入淡出和状态管理调用的协程。
    /// </summary>
    private IEnumerator TransformToScene(Scene sceneToUnload, string toSceneName, string targetTeleporterID)
    {
        IsTransitioning = true; // Set transition flag / 设置转换标志

        // --- Start Fade Out / 开始淡出 ---
        yield return Fade(1f);

        // --- Trigger Pre-Unload Event & Disable Player / 触发卸载前事件并禁用玩家 ---
        EventManager.Instance?.TriggerEvent(new BeforeSceneUnloadEvent());
        Player.Instance?.DisableCollision(); // Disable player collision during transition / 在转换期间禁用玩家碰撞

        // --- Save State of Scene Being Unloaded / 保存正在卸载的场景的状态 ---
        if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
        {
            // --- TYPO FIX HERE ---
            GameSceneManager sceneManagerToSave = FindGameSceneManagerInScene(sceneToUnload);
            // --- END TYPO FIX ---
            if (sceneManagerToSave != null)
            {
                SceneSaveData stateToSave = sceneManagerToSave.SaveCurrentState();
                SessionStateManager.Instance?.RecordSceneState(sceneToUnload.name, stateToSave); // Store state / 存储状态
            } else { Debug.LogWarning($"[TransitionManager] GameSceneManager not found in scene '{sceneToUnload.name}' to save state."); }
        }

        // --- Load/Unload Scenes / 加载/卸载场景 ---
        AsyncOperation loadOperation = null;
        AsyncOperation unloadOperation = null;
        Scene newScene = default; // Store the newly loaded scene reference / 存储新加载的场景引用

        if (sceneToUnload.name != toSceneName) // Only load/unload if changing scenes / 仅在更改场景时加载/卸载
        {
            // 1. Load the new scene additively / 1. 以 Additive 模式加载新场景
            // Debug.Log($"[TransitionManager] Coroutine: Loading '{toSceneName}' additively...");
            loadOperation = SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive);
            if (loadOperation == null) { Debug.LogError($"[TransitionManager] Coroutine: Failed to start loading scene '{toSceneName}'."); yield return HandleTransitionError(); yield break; }
            while (!loadOperation.isDone) yield return null; // Wait for load / 等待加载
            // Debug.Log($"[TransitionManager] Coroutine: Scene '{toSceneName}' loaded.");

            // 2. Set the newly loaded scene active / 2. 将新加载的场景设置为活动场景
            newScene = SceneManager.GetSceneByName(toSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                // Debug.Log($"[TransitionManager] Coroutine: Set '{toSceneName}' as active scene.");
            }
            else { Debug.LogError($"[TransitionManager] Coroutine: Failed to find scene '{toSceneName}' after loading!"); yield return HandleTransitionError(); yield break; }

            // 3. Unload the previous active scene / 3. 卸载之前的活动场景
            // Debug.Log($"[TransitionManager] Coroutine: Attempting to unload previous scene '{sceneToUnload.name}'...");
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload);
                if (unloadOperation == null) { Debug.LogError($"[TransitionManager] Coroutine: Failed to start unloading scene '{sceneToUnload.name}'."); }
            } else { Debug.LogWarning($"[TransitionManager] Coroutine: Scene '{sceneToUnload.name}' invalid or not loaded. Skipping unload."); }
            // Wait for unload if it started / 如果已启动，则等待卸载
            while (unloadOperation != null && !unloadOperation.isDone) yield return null;
            // if (unloadOperation != null) Debug.Log($"[TransitionManager] Coroutine: Scene '{sceneToUnload.name}' unloaded.");
        }
        else // Transitioning within the same scene / 在同一场景内转换
        {
             // Debug.Log("[TransitionManager] Coroutine: Transitioning within the same scene.");
             newScene = sceneToUnload; // Target scene is the same / 目标场景相同
             if (newScene.IsValid()) SceneManager.SetActiveScene(newScene); // Ensure active / 确保活动
        }

        // --- WAIT A FRAME before loading state / 加载状态前等待一帧 ---
        // Allows objects in the new scene (like NPCs) to run Awake/Start and register.
        // 允许新场景中的对象（如 NPC）运行 Awake/Start 并进行注册。
        // Debug.Log($"[TransitionManager] Waiting one frame before applying loaded state to scene '{newScene.name}'...");
        yield return null;
        // --- END WAIT / 结束等待 ---

        // --- Load State for Newly Loaded/Activated Scene / 加载新加载/激活场景的状态 ---
        if (newScene.IsValid())
        {
             // --- TYPO FIX HERE ---
             GameSceneManager sceneManagerToLoad = FindGameSceneManagerInScene(newScene);
             // --- END TYPO FIX ---
             if (sceneManagerToLoad != null)
             {
                  if (SessionStateManager.Instance != null && SessionStateManager.Instance.TryGetSceneState(newScene.name, out SceneSaveData loadedData))
                  {
                       // Debug.Log($"[TransitionManager] Applying saved state to scene: {newScene.name}");
                       sceneManagerToLoad.LoadSaveData(loadedData); // Apply state AFTER the delay / 在延迟后应用状态
                  } else { Debug.Log($"[TransitionManager] No saved state found for scene: {newScene.name}. Initializing fresh."); }
             } else { Debug.LogWarning($"[TransitionManager] GameSceneManager not found in newly loaded/activated scene '{newScene.name}'. Cannot load state.");}
        }

        // --- Trigger Post-Unload Event & Move Player / 触发卸载后事件并移动玩家 ---
        EventManager.Instance?.TriggerEvent(new AfterSceneUnloadEvent());
        yield return null; // Extra frame might help ensure player exists / 额外一帧可能有助于确保玩家存在
        MovePlayerToTarget(targetTeleporterID); // Move player to the destination / 将玩家移动到目的地
        Player.Instance?.EnableCollision(); // Re-enable player collision / 重新启用玩家碰撞

        // --- Start Fade In / 开始淡入 ---
        yield return Fade(0f);

        IsTransitioning = false; // Clear transition flag / 清除转换标志
        // Debug.Log($"[TransitionManager TransformToScene Coroutine] Completed.");
    }

    /// <summary>
    /// Coroutine to handle the fade effect using CanvasGroup alpha.
    /// 使用 CanvasGroup alpha 处理淡入淡出效果的协程。
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break; // Exit if no canvas group / 如果没有画布组则退出
        fadeCanvasGroup.blocksRaycasts = true; // Block raycasts during fade / 在淡入淡出期间阻止射线投射
        float currentAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            // Use unscaledDeltaTime for fades to work during pause / 使用 unscaledDeltaTime 以便在暂停期间淡入淡出有效
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha; // Ensure final alpha value / 确保最终 alpha 值
        // Only block raycasts if significantly visible / 仅在显著可见时阻止射线投射
        fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.1f);
    }

    /// <summary>
    /// Finds the target teleporter and moves the player to its spawn point.
    /// 查找目标传送器并将玩家移动到其生成点。
    /// </summary>
    private void MovePlayerToTarget(string targetTeleporterID)
    {
        // Debug.Log($"[TransitionManager] Attempting to move player to target teleporter ID: '{targetTeleporterID}'");
        ITeleportable targetTeleporter = FindTeleporterWithID(targetTeleporterID); // Find target / 查找目标
        if (targetTeleporter != null)
        {
            if (Player.Instance != null)
            {
                if (targetTeleporter.Spawnpoint != null) // Check if spawn point exists / 检查生成点是否存在
                {
                    Player.Instance.transform.position = targetTeleporter.Spawnpoint.position; // Move player / 移动玩家
                    Player.Instance.ZeroVelocity(); // Stop player movement / 停止玩家移动
                    // Debug.Log($"[TransitionManager] Player moved to spawn point of '{targetTeleporterID}'.");
                } else { Debug.LogError($"[TransitionManager] Target teleporter '{targetTeleporterID}' found, but its Spawnpoint is null!"); }
            } else { Debug.LogError("[TransitionManager] Player.Instance not found! Cannot move player."); }
        } else { Debug.LogError($"[TransitionManager] Target teleporter with ID '{targetTeleporterID}' not found in loaded scene(s)!"); }
    }

    /// <summary>
    /// Finds an ITeleportable component in the loaded scenes with the matching ID.
    /// 在已加载的场景中查找具有匹配 ID 的 ITeleportable 组件。
    /// </summary>
    private ITeleportable FindTeleporterWithID(string teleporterID)
    {
        // Search across all loaded scenes / 在所有已加载的场景中搜索
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
             Scene scene = SceneManager.GetSceneAt(i);
             if (scene.isLoaded)
             {
                  GameObject[] rootObjects = scene.GetRootGameObjects();
                  foreach (GameObject root in rootObjects)
                  {
                       // Find inactive components too / 也查找非活动组件
                       ITeleportable[] teleporters = root.GetComponentsInChildren<ITeleportable>(true);
                       foreach (ITeleportable teleporter in teleporters)
                       {
                            if (teleporter as MonoBehaviour != null && teleporter.TeleportID == teleporterID)
                            {
                                 // Debug.Log($"[TransitionManager] Found target '{teleporterID}' in scene '{scene.name}'.");
                                 return teleporter;
                            }
                       }
                  }
             }
        }
        // Debug.LogError($"[TransitionManager] Failed to find ITeleportable with ID '{teleporterID}'.");
        return null;
    }

    /// <summary>
    /// Finds the GameSceneManager component within the root objects of a given scene.
    /// 在给定场景的根对象中查找 GameSceneManager 组件。
    /// </summary>
    /// <param name="scene">The scene to search within. / 要搜索的场景。</param>
    /// <returns>The found GameSceneManager, or null if not found. / 找到的 GameSceneManager，如果未找到则为 null。</returns>
    private GameSceneManager FindGameSceneManagerInScene(Scene scene)
    {
         if (!scene.IsValid() || !scene.isLoaded) {
              Debug.LogWarning($"[TransitionManager] FindGameSceneManagerInScene called with invalid or unloaded scene: {scene.name}");
              return null;
         }
         GameObject[] rootObjects = scene.GetRootGameObjects();
         foreach (GameObject rootObject in rootObjects)
         {
              // Search including inactive GameObjects / 搜索包括非活动 GameObject
              GameSceneManager manager = rootObject.GetComponentInChildren<GameSceneManager>(true);
              if (manager != null) { return manager; }
         }
         // Debug.LogWarning($"[TransitionManager] GameSceneManager component not found in the root objects of scene: {scene.name}");
         return null;
    }


    /// <summary>
    /// Handles errors during the transition coroutine, attempting to recover.
    /// 处理转换协程期间的错误，尝试恢复。
    /// </summary>
    private IEnumerator HandleTransitionError()
    {
         Debug.LogError($"[TransitionManager] An error occurred during scene transition. Attempting to recover.");
         Player.Instance?.EnableCollision(); // Re-enable player collision / 重新启用玩家碰撞
         yield return Fade(0f); // Fade back in / 淡入
         IsTransitioning = false; // Allow trying again? / 允许重试？
         // Consider forcing back to main menu / 考虑强制返回主菜单
         // GameRunManager.Instance?.EndGameSession();
    }
}
