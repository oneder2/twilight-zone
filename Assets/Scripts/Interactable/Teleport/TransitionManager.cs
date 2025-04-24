// File: Scripts/Interactable/Teleport/TransitionManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages scene transitions between levels within the game session.
/// 管理游戏会话中关卡之间的场景转换。
/// Handles additive loading/unloading, fade effects, and coordinates session state saving/loading.
/// 处理附加加载/卸载、淡入淡出效果，并协调会话状态的保存/加载。
/// Assumes it resides in the persistent GameRoot scene.
/// 假设它驻留在持久性 GameRoot 场景中。
/// </summary>
public class TransitionManager : Singleton<TransitionManager> // Assuming you have a Singleton base class // 假设你有一个 Singleton 基类
{
    [Header("Fade Effect Settings")]
    [Tooltip("CanvasGroup used for the fade effect.")]
    // [Tooltip("用于淡入淡出效果的 CanvasGroup。")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of the fade in/out animation in seconds.")]
    // [Tooltip("淡入/淡出动画的持续时间（秒）。")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Scene Configuration")]
    [Tooltip("Name of the scene containing core game managers (this manager). Used for safety checks.")]
    // [Tooltip("包含核心游戏管理器（此管理器）的场景名称。用于安全检查。")]
    [SerializeField] private string gameRootSceneName = "GameRoot"; // Important for checks // 对检查很重要

    /// <summary>
    /// Gets whether a fade transition is currently active.
    /// 获取淡入淡出转换当前是否处于活动状态。
    /// </summary>
    public bool IsTransitioning { get; private set; } = false;

    // Ensure fade canvas starts correctly
    // 确保淡入淡出画布正确启动
    protected override void Awake()
    {
        // --- LOGGING ADDED ---
        // --- 已添加日志 ---
        Debug.Log($"[TransitionManager Awake] Instance ID: {GetInstanceID()}, GO Name: {gameObject.name}, Scene: {gameObject.scene.name}");
        base.Awake(); // Call base Singleton Awake *after* logging // 在记录日志*之后*调用基类 Singleton Awake
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[TransitionManager] Fade Canvas Group is not assigned in TransitionManager!");
            return;
        }
        // Initialize fade state only if this instance survives the Singleton check
        // 仅当此实例在 Singleton 检查中存活下来时才初始化淡入淡出状态
        if (Instance == this) // Check if this is the chosen singleton instance // 检查这是否是选定的单例实例
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    // --- Event Subscription for Event-Driven Transitions (Optional but Recommended) ---
    // --- 事件驱动转换的事件订阅（可选但推荐）---
    void OnEnable()
    {
        // --- LOGGING ADDED ---
        // --- 已添加日志 ---
        Debug.Log($"[TransitionManager OnEnable] Instance ID: {GetInstanceID()}, GO Name: {gameObject.name}");
        if (EventManager.Instance != null)
        {
             // Check if already subscribed? Less critical with lambda removal later.
             // 检查是否已订阅？稍后移除 lambda 后不那么重要。
             EventManager.Instance.AddListener<TransitionRequestedEvent>(HandleTransitionRequest);
             Debug.Log($"[TransitionManager OnEnable] Subscribed HandleTransitionRequest (Instance ID: {GetInstanceID()})");
        } else { Debug.LogError("[TransitionManager OnEnable] EventManager not found!"); }
    }
    void OnDisable()
    {
         // --- LOGGING ADDED ---
         // --- 已添加日志 ---
         Debug.Log($"[TransitionManager OnDisable] Instance ID: {GetInstanceID()}, GO Name: {gameObject.name}");
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<TransitionRequestedEvent>(HandleTransitionRequest);
             Debug.Log($"[TransitionManager OnDisable] Unsubscribed HandleTransitionRequest (Instance ID: {GetInstanceID()})");
        } else { Debug.LogWarning("[TransitionManager OnDisable] EventManager not found during cleanup."); }
    }
     void OnDestroy()
     {
         // --- LOGGING ADDED ---
         // --- 已添加日志 ---
         Debug.Log($"[TransitionManager OnDestroy] Instance ID: {GetInstanceID()}, GO Name: {gameObject.name}");
         // Note: OnDisable should have already unsubscribed the listener.
         // 注意：OnDisable 应该已经取消订阅了监听器。
     }


    private void HandleTransitionRequest(TransitionRequestedEvent eventData)
    {
        // --- LOGGING ADDED ---
        // --- 已添加日志 ---
        Debug.Log($"[TransitionManager HandleTransitionRequest] Event received by Instance ID: {GetInstanceID()}. Requesting teleport to '{eventData?.TargetSceneName ?? "NULL"}', TargetID '{eventData?.TargetTeleporterID ?? "NULL"}'");
        if (eventData == null) return;
        InitiateTeleport(eventData.TargetSceneName, eventData.TargetTeleporterID);
    }
    // --- End Event Subscription ---


    /// <summary>
    /// Public method to initiate the teleport sequence. Can be called directly or via event handler.
    /// 启动传送序列的公共方法。可以直接调用或通过事件处理程序调用。
    /// </summary>
    /// <param name="toSceneName">The name of the target scene to load. / 要加载的目标场景的名称。</param>
    /// <param name="targetTeleporterID">The ID of the teleporter to spawn at in the target scene. / 目标场景中要生成的传送器的 ID。</param>
    public void InitiateTeleport(string toSceneName, string targetTeleporterID)
    {
         // --- LOGGING ADDED ---
         // --- 已添加日志 ---
         Debug.Log($"[TransitionManager InitiateTeleport] Method entered on Instance ID: {GetInstanceID()}. IsTransitioning: {IsTransitioning}");

         // --- Explicit Null/Destroyed Check ---
         // --- 显式空值/已销毁检查 ---
         if (this == null || !this) // Check both explicit and implicit null/destroyed state // 检查显式和隐式空值/已销毁状态
         {
             Debug.LogError($"[TransitionManager InitiateTeleport] Instance (ID: {GetInstanceID()}) is detected as NULL or DESTROYED before starting coroutine! Aborting teleport.");
             return;
         }
         // --- End Check ---


        if (IsTransitioning)
        {
            Debug.LogWarning($"[TransitionManager InitiateTeleport] Teleport requested on Instance ID: {GetInstanceID()} while already transitioning. Ignoring request.");
            return;
        }

        Scene sceneToUnload = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(toSceneName)) { Debug.LogError($"[TransitionManager InitiateTeleport] Teleport requested on Instance ID: {GetInstanceID()} with an empty target scene name!"); return; }
        if (sceneToUnload.name == gameRootSceneName) { Debug.LogError($"[TransitionManager InitiateTeleport] Attempting to start transition FROM the GameRoot scene ('{gameRootSceneName}') on Instance ID: {GetInstanceID()}? Aborting."); return; }

        Debug.Log($"[TransitionManager InitiateTeleport] Starting teleport coroutine on Instance ID: {GetInstanceID()}. From: '{sceneToUnload.name}' To: '{toSceneName}' Target ID: '{targetTeleporterID}'");
        StartCoroutine(TransformToScene(sceneToUnload, toSceneName, targetTeleporterID)); // Error occurs here // 此处发生错误
    }

    /// <summary>
    /// Coroutine that handles the actual scene loading, unloading, fading, and state management calls.
    /// 处理实际场景加载、卸载、淡入淡出和状态管理调用的协程。
    /// </summary>
    private IEnumerator TransformToScene(Scene sceneToUnload, string toSceneName, string targetTeleporterID)
    {
        // --- LOGGING ADDED ---
        // --- 已添加日志 ---
        Debug.Log($"[TransitionManager TransformToScene Coroutine] Started on Instance ID: {GetInstanceID()}.");
        IsTransitioning = true;

        // --- Start Fade Out ---
        // --- 开始淡出 ---
        yield return Fade(1f);

        // --- Trigger Pre-Unload Event & Disable Player ---
        // --- 触发卸载前事件并禁用玩家 ---
        if (EventManager.Instance != null) EventManager.Instance.TriggerEvent(new BeforeSceneUnloadEvent());
        if (Player.Instance != null) Player.Instance.DisableCollision(); else Debug.LogWarning("[TransitionManager] Player.Instance not found during transition start.");

        // --- *** SAVE STATE of Scene Being Unloaded *** ---
        // --- *** 保存正在卸载的场景的状态 *** ---
        if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
        {
            GameSceneManager sceneManagerToSave = FindGameSceneManagerInScene(sceneToUnload);
            if (sceneManagerToSave != null)
            {
                SceneSaveData stateToSave = sceneManagerToSave.SaveCurrentState();
                if (SessionStateManager.Instance != null)
                {
                    SessionStateManager.Instance.RecordSceneState(sceneToUnload.name, stateToSave);
                } else { Debug.LogError("[TransitionManager] SessionStateManager not found! Cannot save scene state."); }
            } else { Debug.LogWarning($"[TransitionManager] GameSceneManager not found in scene '{sceneToUnload.name}'. Cannot save state.");}
        }
        // --- *** End Save State *** ---

        AsyncOperation loadOperation = null;
        AsyncOperation unloadOperation = null;
        Scene newScene = default; // Store the newly loaded scene reference // 存储新加载的场景引用

        // --- Load/Unload Scenes only if changing scenes ---
        // --- 仅在更改场景时加载/卸载场景 ---
        if (sceneToUnload.name != toSceneName)
        {
            // 1. Load the new scene additively
            // 1. 以 Additive 模式加载新场景
            Debug.Log($"[TransitionManager] Coroutine: Loading '{toSceneName}' additively...");
            loadOperation = SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive);
            if (loadOperation == null) { Debug.LogError($"[TransitionManager] Coroutine: Failed to start loading scene '{toSceneName}'."); yield return HandleTransitionError(); yield break; }
            while (!loadOperation.isDone) yield return null;
            Debug.Log($"[TransitionManager] Coroutine: Scene '{toSceneName}' loaded.");

            // 2. Set the newly loaded scene active
            // 2. 将新加载的场景设置为活动场景
            newScene = SceneManager.GetSceneByName(toSceneName); // Get loaded scene reference // 获取加载的场景引用
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene); // Actually set the scene active // 实际设置场景为活动场景
                Debug.Log($"[TransitionManager] Coroutine: Set '{toSceneName}' as active scene.");
            }
            else
            {
                 Debug.LogError($"[TransitionManager] Coroutine: Failed to find scene '{toSceneName}' by name after loading! Cannot set active scene.");
                 yield return HandleTransitionError();
                 yield break;
            }

            // 3. Unload the previous active scene
            // 3. 卸载之前的活动场景
            Debug.Log($"[TransitionManager] Coroutine: Attempting to unload previous scene '{sceneToUnload.name}'...");
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload); // Use the Scene object // 使用 Scene 对象
                if (unloadOperation == null) { Debug.LogError($"[TransitionManager] Coroutine: Failed to start unloading scene '{sceneToUnload.name}'."); }
            }
            else { Debug.LogWarning($"[TransitionManager] Coroutine: Scene '{sceneToUnload.name}' was invalid or not loaded. Skipping unload."); }
            // Wait for unload if it started
            // 如果已启动，则等待卸载
            while (unloadOperation != null && !unloadOperation.isDone) yield return null;
            if (unloadOperation != null) Debug.Log($"[TransitionManager] Coroutine: Scene '{sceneToUnload.name}' unloaded.");

        }
        else
        {
            Debug.Log("[TransitionManager] Coroutine: Transitioning within the same scene (no scene load/unload).");
            newScene = sceneToUnload; // Target scene is the same as the one we started in // 目标场景与我们开始时的场景相同
            if (newScene.IsValid()) SceneManager.SetActiveScene(newScene); // Ensure it's active just in case // 确保它是活动的，以防万一
        }

        // --- *** LOAD STATE for Newly Loaded/Activated Scene *** ---
        // --- *** 加载新加载/激活场景的状态 *** ---
        if (newScene.IsValid()) // Ensure we have a valid target scene // 确保我们有一个有效的目标场景
        {
             yield return null; // Wait a frame for objects in new scene to potentially Awake/Start // 等待一帧，以便新场景中的对象可能执行 Awake/Start
             GameSceneManager sceneManagerToLoad = FindGameSceneManagerInScene(newScene);
             if (sceneManagerToLoad != null)
             {
                  if (SessionStateManager.Instance != null && SessionStateManager.Instance.TryGetSceneState(newScene.name, out SceneSaveData loadedData))
                  {
                       Debug.Log($"[TransitionManager] Applying saved state to scene: {newScene.name}");
                       sceneManagerToLoad.LoadSaveData(loadedData);
                  } else {
                       Debug.Log($"[TransitionManager] No saved state found for scene: {newScene.name}. Initializing fresh.");
                  }
             } else { Debug.LogWarning($"[TransitionManager] GameSceneManager not found in newly loaded/activated scene '{newScene.name}'. Cannot load state.");}
        }
        // --- *** End Load State *** ---


        // --- Trigger Post-Unload Event & Move Player ---
        // --- 触发卸载后事件并移动玩家 ---
        if (EventManager.Instance != null) EventManager.Instance.TriggerEvent(new AfterSceneUnloadEvent());
        yield return null;
        MovePlayerToTarget(targetTeleporterID);
        if (Player.Instance != null) Player.Instance.EnableCollision(); else Debug.LogWarning("[TransitionManager] Player.Instance not found during transition end.");

        // --- Start Fade In ---
        // --- 开始淡入 ---
        yield return Fade(0f);

        IsTransitioning = false;
        Debug.Log($"[TransitionManager TransformToScene Coroutine] Completed on Instance ID: {GetInstanceID()}.");
    }

    // --- Helper function to find GameSceneManager within a specific scene ---
    // --- 在特定场景中查找 GameSceneManager 的辅助函数 ---
    private GameSceneManager FindGameSceneManagerInScene(Scene scene)
    {
         if (!scene.IsValid() || !scene.isLoaded) { Debug.LogWarning($"[TransitionManager] FindGameSceneManagerInScene called with invalid or unloaded scene: {scene.name}"); return null; }
         GameObject[] rootObjects = scene.GetRootGameObjects();
         foreach (GameObject rootObject in rootObjects)
         {
              GameSceneManager manager = rootObject.GetComponentInChildren<GameSceneManager>(true);
              if (manager != null) { return manager; }
         }
          // Debug.LogWarning($"[TransitionManager] GameSceneManager component not found in the root objects of scene: {scene.name}"); // Less verbose // 减少冗余
         return null;
    }


    // --- Other methods remain the same: Fade, MovePlayerToTarget, FindTeleporterWithID, HandleTransitionError ---
    // --- 其他方法保持不变：Fade、MovePlayerToTarget、FindTeleporterWithID、HandleTransitionError ---
    // ... (Paste the implementations for Fade, MovePlayerToTarget, FindTeleporterWithID, HandleTransitionError here from the previous version) ...

    /// <summary>
    /// Coroutine to handle the fade effect.
    /// 处理淡入淡出效果的协程。
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true;
        float currentAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Use unscaledDeltaTime for fades to work during pause
            // 使用 unscaledDeltaTime 以便在暂停期间淡入淡出有效
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.1f);
    }

    /// <summary>
    /// Finds the target teleporter and moves the player to its spawn point.
    /// 查找目标传送器并将玩家移动到其生成点。
    /// </summary>
    private void MovePlayerToTarget(string targetTeleporterID)
    {
        Debug.Log($"[TransitionManager MovePlayerToTarget] Attempting to move player to target teleporter ID: '{targetTeleporterID}' on Instance ID: {GetInstanceID()}");
        ITeleportable targetTeleporter = FindTeleporterWithID(targetTeleporterID);
        if (targetTeleporter != null)
        {
            if (Player.Instance != null)
            {
                if (targetTeleporter.Spawnpoint != null)
                {
                    Player.Instance.transform.position = targetTeleporter.Spawnpoint.position;
                    Player.Instance.ZeroVelocity(); // Ensure player stops after teleport // 确保玩家传送后停止
                    Debug.Log($"[TransitionManager MovePlayerToTarget] Player successfully moved to spawn point of '{targetTeleporterID}' at {targetTeleporter.Spawnpoint.position}");
                } else { Debug.LogError($"[TransitionManager MovePlayerToTarget] Target teleporter '{targetTeleporterID}' found, but its Spawnpoint transform is null!"); }
            } else { Debug.LogError("[TransitionManager MovePlayerToTarget] Player.Instance not found! Cannot move player."); }
        } else { Debug.LogError($"[TransitionManager MovePlayerToTarget] Target teleporter with ID '{targetTeleporterID}' not found in the newly loaded scene(s)!"); }
    }

    /// <summary>
    /// Finds an ITeleportable component in the loaded scenes with the matching ID.
    /// 在已加载的场景中查找具有匹配 ID 的 ITeleportable 组件。
    /// </summary>
    private ITeleportable FindTeleporterWithID(string teleporterID)
    {
        // Search across all loaded scenes, not just the active one
        // 在所有已加载的场景中搜索，而不仅仅是活动场景
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
             Scene scene = SceneManager.GetSceneAt(i);
             if (scene.isLoaded)
             {
                  GameObject[] rootObjects = scene.GetRootGameObjects();
                  foreach (GameObject root in rootObjects)
                  {
                       // Find inactive components too, in case the target door/ladder starts disabled
                       // 也查找非活动组件，以防目标门/梯子开始时被禁用
                       ITeleportable[] teleporters = root.GetComponentsInChildren<ITeleportable>(true);
                       foreach (ITeleportable teleporter in teleporters)
                       {
                            // Use MonoBehaviour null check as ITeleportable might be on a destroyed object
                            // 使用 MonoBehaviour 空值检查，因为 ITeleportable 可能位于已销毁的对象上
                            if (teleporter as MonoBehaviour != null && teleporter.TeleportID == teleporterID)
                            {
                                 Debug.Log($"[TransitionManager FindTeleporterWithID] Found target '{teleporterID}' in scene '{scene.name}' on GameObject '{(teleporter as MonoBehaviour).gameObject.name}'");
                                 return teleporter;
                            }
                       }
                  }
             }
        }
        Debug.LogError($"[TransitionManager FindTeleporterWithID] Failed to find any ITeleportable with ID '{teleporterID}' across all loaded scenes.");
        return null;
    }

    /// <summary>
    /// Handles errors during the transition coroutine.
    /// 处理转换协程期间的错误。
    /// </summary>
    private IEnumerator HandleTransitionError()
    {
         Debug.LogError($"[TransitionManager HandleTransitionError] An error occurred during scene transition on Instance ID: {GetInstanceID()}. Attempting to recover.");
         if (Player.Instance != null) Player.Instance.EnableCollision();
         yield return Fade(0f); // Fade back in // 淡入
         IsTransitioning = false; // Allow trying again? // 允许重试？
         // Consider forcing back to main menu or a safe state
         // 考虑强制返回主菜单或安全状态
         // if(GameRunManager.Instance != null) GameRunManager.Instance.EndGameSession();
    }

} // End of TransitionManager class // TransitionManager 类结束
