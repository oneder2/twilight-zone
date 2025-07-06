using UnityEngine;
using UnityEngine.Playables; // 需要 Playables


// 游戏状态枚举 (Game Status Enum)
public enum GameStatus {
    InMenu,       // 在主菜单 (In the main menu)
    Loading,      // 加载中 (Loading transition)
    Playing,      // 游戏中 (Normal gameplay)
    Paused,       // 游戏暂停 (Game paused by user)
    InCutscene,   // 播放过场动画/CG中 (Playing a cutscene/CG)
    GameOver,     // 游戏结束 (Game over screen)
    InDialogue    // 显示对话中 (Displaying dialogue)
}

/// <summary>
/// 管理全局游戏运行状态，处理暂停、场景加载锁、Timeline控制等。
/// Manages the overall game running state, handles pausing, scene loading locks,
/// and Timeline control. Dialogue-related Timeline pausing is now handled by custom tracks.
/// 应该是持久化的 Singleton (例如位于 "Boot" 场景)。
/// Should be a persistent Singleton (e.g., in the "Boot" scene).
/// </summary>
public class GameRunManager : Singleton<GameRunManager> // Ensure Singleton<T> is correctly implemented
{
    #region Inspector Fields
    [Header("状态管理 (State Management)")]
    [Tooltip("当前游戏状态 (Read Only) / Current game status (Read Only)")]
    [SerializeField] // 只在 Inspector 中显示，不允许修改
    private GameStatus _currentStatus = GameStatus.InMenu;

    [Header("场景加载状态 (Scene Loading Status)")]
    [Tooltip("指示场景加载或卸载过程当前是否处于活动状态 (Read Only) / Indicates if a scene loading or unloading process is currently active (Read Only)")]
    [SerializeField] // 只在 Inspector 中显示
    private bool _isSceneLoadingOrUnloading = false;

    [Header("Timeline 设置 (Timeline Settings)")]
    [Tooltip("(可选) 用于过场动画的主 PlayableDirector 引用 / (Optional) Reference to the main PlayableDirector used for cutscenes")]
    [SerializeField] private PlayableDirector mainCutsceneDirector;
    #endregion

    #region Public Properties
    /// <summary>
    /// 获取当前的游戏状态。
    /// Gets the current game status.
    /// </summary>
    public GameStatus CurrentStatus => _currentStatus;

    /// <summary>
    /// 检查游戏是否处于用户暂停状态。
    /// Checks if the game is currently paused by the user.
    /// </summary>
    public bool IsPaused => _currentStatus == GameStatus.Paused;

    /// <summary>
    /// 检查游戏是否正在播放过场动画。
    /// Checks if a cutscene is currently active.
    /// </summary>
    public bool IsCutsceneActive => _currentStatus == GameStatus.InCutscene;

    /// <summary>
    /// 获取场景是否正在加载或卸载中。
    /// Gets whether a scene is currently being loaded or unloaded.
    /// </summary>
    public bool IsSceneLoadingOrUnloading => _isSceneLoadingOrUnloading;

    /// <summary>
    /// (静态) 跟踪此会话中初始游戏加载序列是否已完成。
    /// (Static) Tracks if the initial game loading sequence has completed in this session.
    /// </summary>
    public static bool IsInitialLoadComplete { get; private set; } = false;

    /// <summary>
    /// (静态) 指示游戏结束后是否应自动重新开始的标志。由 GameOverUI 或其他逻辑设置。
    /// (Static) Flag indicating if the game should automatically restart after ending. Set by GameOverUI or other logic.
    /// </summary>
    public static bool InitiateRestartFlow { get; set; } = false; // Made property with setter
    #endregion


    #region Unity Lifecycle Methods
    protected override void Awake()
    {
        base.Awake(); // 处理 Singleton 逻辑
        // 初始化状态
        _currentStatus = GameStatus.InMenu; // 假定从主菜单开始
        _isSceneLoadingOrUnloading = false;
        // currentlyPausedDirector = null; // Removed
        IsInitialLoadComplete = false; // 每次程序启动时重置
        InitiateRestartFlow = false; // 每次程序启动时重置
        Debug.Log($"[GameRunManager Awake] Initialized. Status: {_currentStatus}");
    }

     void Start()
     {
         // 确保 EventManager 存在并注册监听器 (只注册必要的)
         // Ensure EventManager exists and register necessary listeners
         RegisterEventListeners();

         // 播放初始音乐（如果需要且未播放）
         // Play initial music (if needed and not already playing)
         if (_currentStatus == GameStatus.InMenu && AudioManager.Instance != null && AudioManager.Instance.GetCurrentTrack() == MusicTrack.None)
         {
              AudioManager.Instance.PlayMusic(MusicTrack.MainMenuTheme);
         }
     }

    void Update()
    {
        // 处理全局 Timeline 跳过输入
        // Handle global Timeline skip input
        HandleTimelineSkipInput();
    }

    void OnDestroy() // 或 OnDisable，如果 Singleton 基类在那里清理
    {
         // 取消注册监听器
         // Unsubscribe listeners
         UnregisterEventListeners();
    }
    #endregion

    #region Event Handling
    /// <summary>
    /// 注册需要监听的全局事件。(移除了对话暂停/恢复相关监听)
    /// Registers listeners for required global events. (Removed dialogue pause/resume listeners)
    /// </summary>
    private void RegisterEventListeners()
    {
        if (EventManager.Instance != null)
        {
            // Add other listeners if needed here...
            // 如果需要，在此处添加其他监听器...
            Debug.Log("[GameRunManager] Event listeners registered (dialogue pause/resume removed).");
        }
        else
        {
            Debug.LogError("[GameRunManager] EventManager not found on Start!");
        }
    }

    /// <summary>
    /// 取消注册所有监听的全局事件。(移除了对话暂停/恢复相关监听)
    /// Unregisters all subscribed global event listeners. (Removed dialogue pause/resume listeners)
    /// </summary>
    private void UnregisterEventListeners()
    {
        // 检查 EventManager 是否还存在
        // Check if EventManager still exists
        if (EventManager.Instance != null)
        {
            // Remove other listeners if needed here...
            // 如果需要，在此处移除其他监听器...
             Debug.Log("[GameRunManager] Event listeners unregistered (dialogue pause/resume removed).");
        }
    }


    #endregion

    #region Public State Control Methods
    /// <summary>
    /// 改变全局游戏状态，处理相关逻辑并触发事件。
    /// Changes the global game status, handles related logic, and triggers an event.
    /// </summary>
    /// <param name="newStatus">要切换到的新状态 (The new status to switch to)</param>
    public void ChangeGameStatus(GameStatus newStatus)
    {
        if (_currentStatus == newStatus) return; // 状态未改变，无需操作

        GameStatus previousStatus = _currentStatus;
        _currentStatus = newStatus;
        Debug.Log($"[GameRunManager] Game Status Changed: {previousStatus} -> {_currentStatus}");

        // 处理状态改变带来的副作用（例如时间缩放）
        HandleStatusChangeSideEffects(previousStatus, newStatus);

        // 触发全局状态变更事件
        EventManager.Instance?.TriggerEvent(new GameStatusChangedEvent(_currentStatus, previousStatus));
    }

    /// <summary>
    /// 由 Timeline Signal Proxy 调用，进入过场动画状态。
    /// Called by Timeline Signal Proxy to enter the cutscene state.
    /// </summary>
    public void EnterCutsceneState()
    {
        // 只有在非加载、非游戏结束等状态下才应该进入过场
        if (_currentStatus == GameStatus.Playing || _currentStatus == GameStatus.InDialogue) // Or other valid states
        {
            ChangeGameStatus(GameStatus.InCutscene);
        } else {
            Debug.LogWarning($"[GameRunManager] EnterCutsceneState called but current status is {_currentStatus}. Ignoring.");
        }
    }

    /// <summary>
    /// 由 Timeline Signal Proxy 调用，退出过场动画状态。
    /// Called by Timeline Signal Proxy to exit the cutscene state.
    /// </summary>
    public void ExitCutsceneState()
    {
        // 只有在确实处于过场动画状态时才退出
        if (_currentStatus == GameStatus.InCutscene)
        {
            // No longer need to check for currentlyPausedDirector here.
            // 此处不再需要检查 currentlyPausedDirector。
            ChangeGameStatus(GameStatus.Playing);
        } else {
            Debug.LogWarning($"[GameRunManager] ExitCutsceneState called but CurrentStatus is {_currentStatus}. Not changing to Playing.");
        }
    }

    /// <summary>
    /// 开始一个新的游戏会话（通常在加载第一个关卡之前调用）。
    /// Starts a new game session (typically called before loading the first level).
    /// </summary>
    public void StartGameSession()
    {
        if (_currentStatus == GameStatus.InMenu || _currentStatus == GameStatus.GameOver)
        {
            Debug.Log("[GameRunManager] StartGameSession called, preparing for game start.");
            IsInitialLoadComplete = false;
            EventManager.Instance?.ResetTimeEvents();
            ProgressManager.Instance?.ResetProgress();
            SessionStateManager.Instance?.ClearAllSceneStates();
            // currentlyPausedDirector = null; // Removed

            // Status will be set to Loading by StartGameManager during scene load
        }
        else
        {
             Debug.LogWarning($"[GameRunManager] StartGameSession called but current status is {_currentStatus}.");
        }
    }

    /// <summary>
    /// 结束当前游戏会话（例如返回主菜单）。
    /// Ends the current game session (e.g., returning to the main menu).
    /// </summary>
    public void EndGameSession()
    {
        Debug.Log("[GameRunManager] Ending Game Session...");

        // --- REMOVED Check for currentlyPausedDirector ---
        // if (currentlyPausedDirector != null) { ... }
        // --- END REMOVED ---

        // Stop any active timeline if still in cutscene state
        // 如果仍在过场动画状态，则停止任何活动的 timeline
        if (IsCutsceneActive)
        {
             Debug.LogWarning("[GameRunManager] Ending session while a cutscene was active. Stopping active director.");
             PlayableDirector activeDirector = FindActiveDirector();
             activeDirector?.Stop();
        }

        // Stop current music and play menu music
        if (AudioManager.Instance != null) {
            AudioManager.Instance.StopMusic(false);
            AudioManager.Instance.PlayMusic(MusicTrack.MainMenuTheme);
        } else { Debug.LogWarning("[GameRunManager] AudioManager not found during EndGameSession."); }

        // Reset timed events and scene states
        EventManager.Instance?.ResetTimeEvents();
        SessionStateManager.Instance?.ClearAllSceneStates();

        // Restore time scale and set status to InMenu
        Time.timeScale = 1f;
        ChangeGameStatus(GameStatus.InMenu);

        Debug.Log("[GameRunManager] Game Session Ended. Status set to InMenu.");
    }

    /// <summary>
    /// 暂停游戏（用户操作）。
    /// Pauses the game (user action).
    /// </summary>
    public void PauseGame()
    {
         if (_isSceneLoadingOrUnloading) { Debug.LogWarning("[GameRunManager] Cannot pause while loading/unloading scenes."); return; }

        // Allow pausing during Playing or InCutscene states
        if (_currentStatus == GameStatus.Playing || _currentStatus == GameStatus.InCutscene || _currentStatus == GameStatus.InDialogue) // Allow pausing during dialogue too
        {
            ChangeGameStatus(GameStatus.Paused);
            // Time.timeScale = 0 handled in HandleStatusChangeSideEffects
        }
        else
        {
             Debug.LogWarning($"[GameRunManager] PauseGame called but current status is {_currentStatus}.");
        }
    }

    /// <summary>
    /// 恢复游戏（用户操作）。
    /// Resumes the game (user action).
    /// </summary>
    public void ResumeGame()
    {
         if (_isSceneLoadingOrUnloading) { Debug.LogWarning("[GameRunManager] Cannot resume while loading/unloading scenes."); return; }

        if (_currentStatus == GameStatus.Paused)
        {
            // Simplified approach: Always resume to Playing.
            // If resuming from a paused cutscene, the director should resume automatically when timeScale becomes > 0.
            // 简化方案：总是恢复到 Playing。
            // 如果从暂停的过场动画恢复，当 timeScale > 0 时，director 应该自动恢复。
            ChangeGameStatus(GameStatus.Playing); // Time.timeScale restored in HandleStatusChangeSideEffects
        }
         else
        {
             Debug.LogWarning($"[GameRunManager] ResumeGame called but current status is {_currentStatus}, not Paused.");
        }
    }

    /// <summary>
    /// 设置场景加载/卸载状态锁。
    /// Sets the scene loading/unloading status lock.
    /// </summary>
    /// <param name="isLoading">是否正在加载/卸载 (Is loading/unloading active?)</param>
    public void SetLoadingStatus(bool isLoading)
    {
        _isSceneLoadingOrUnloading = isLoading;
        Debug.Log($"[GameRunManager] IsSceneLoadingOrUnloading set to: {isLoading} (Current Status: {_currentStatus})");
    }

    /// <summary>
    /// 标记初始游戏加载完成。
    /// Marks the initial game load as complete.
    /// </summary>
    public void MarkInitialLoadComplete()
    {
        IsInitialLoadComplete = true;
        Debug.Log("[GameRunManager] Initial game load marked as complete for this session.");
    }
    #endregion

    #region Timeline Control
    /// <summary>
    /// 处理 Timeline 跳过输入。现在区分对话激活状态。
    /// Handles input for skipping Timelines. Now differentiates based on dialogue active state.
    /// </summary>
    private void HandleTimelineSkipInput()
    {
        // 仅当在过场动画中且不在加载时处理
        if (_currentStatus == GameStatus.InCutscene && !_isSceneLoadingOrUnloading)
        {
            // 检查 DialogueManager 是否存在且处于激活状态
            bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

            // 如果对话未激活，允许按键跳过整个 Timeline
            if (!isDialogueActive)
            {
                // 使用 Escape 或 Space (如果 Space 也用于跳过整个CG)
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log("[GameRunManager] Skip entire cutscene input detected (Dialogue not active).");
                    SkipEntireCutscene(); // 调用跳过整个 CG 的方法
                }
            }
            // else: 对话激活时，跳过键由 DialogueManager 处理
        }
    }

    /// <summary>
    /// 跳过当前整个过场动画/Timeline。
    /// Skips the current entire cutscene/Timeline.
    /// </summary>
    public void SkipEntireCutscene()
    {
        Debug.Log("[GameRunManager] Attempting to skip entire cutscene...");

        // --- REMOVED Check for currentlyPausedDirector ---

        // 查找并停止当前播放的 Director
        PlayableDirector activeDirector = FindActiveDirector();
        if (activeDirector != null)
        {
            Debug.Log($"[GameRunManager] Stopping active director: {activeDirector.playableAsset?.name ?? "Unnamed"} for skip.");
            activeDirector.Stop();
        } else {
             Debug.Log("[GameRunManager] No active director found to stop during skip.");
        }

        // 强制退出 Cutscene 状态并恢复到 Playing
        if (_currentStatus == GameStatus.InCutscene)
        {
            Debug.Log("[GameRunManager] Forcing state back to Playing after skipping cutscene.");
            ExitCutsceneState(); // ExitCutsceneState 会将状态改为 Playing
        } else {
             Debug.LogWarning($"[GameRunManager] SkipEntireCutscene called but status was already {_currentStatus}. Ensuring Playing state.");
             if (_currentStatus != GameStatus.Playing) {
                 ChangeGameStatus(GameStatus.Playing);
             }
        }
    }

    /// <summary>
    /// 查找当前正在播放的 PlayableDirector。
    /// Finds the currently playing PlayableDirector.
    /// </summary>
    /// <returns>活动的 PlayableDirector，如果找不到则返回 null。(The active PlayableDirector, or null if none found.)</returns>
    private PlayableDirector FindActiveDirector()
    {
        // 优先检查 mainCutsceneDirector (如果设置了)
        if (mainCutsceneDirector != null && mainCutsceneDirector.state == PlayState.Playing)
        {
            return mainCutsceneDirector;
        }

        // 遍历场景查找其他正在播放的 Director
        var directors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
        foreach (var director in directors)
        {
            if (director != mainCutsceneDirector && director.state == PlayState.Playing && director.playableAsset != null)
            {
                return director;
            }
        }
        return null; // 没有找到活动的 Director
    }
    #endregion

    #region Internal State Logic
    /// <summary>
    /// 处理游戏状态改变时需要执行的副作用逻辑（例如时间缩放）。
    /// Handles side-effect logic needed when the game status changes (e.g., time scale).
    /// </summary>
    private void HandleStatusChangeSideEffects(GameStatus oldStatus, GameStatus newStatus)
    {
        // --- 时间缩放管理 (Time Scale Management) ---
        if (newStatus == GameStatus.Paused)
        {
            Time.timeScale = 0f;
            Debug.Log("[GameRunManager] Time scale set to 0 (Paused).");
        }
        else if (oldStatus == GameStatus.Paused && newStatus != GameStatus.Paused)
        {
            Time.timeScale = 1f;
            Debug.Log("[GameRunManager] Time scale restored to 1 (Resumed).");
        }
        else if (newStatus != GameStatus.Paused && newStatus != GameStatus.Loading && Time.timeScale != 1f)
        {
             Debug.LogWarning($"[GameRunManager] Time scale was {Time.timeScale}, resetting to 1 for status {newStatus}");
             Time.timeScale = 1f;
        }

        // --- 其他状态特定逻辑 (Other Status-Specific Logic) ---
        if (oldStatus == GameStatus.InCutscene && newStatus == GameStatus.Playing)
        {
             DialogueManager.Instance.HideDialogue();
             Time.timeScale = 1f; // 确保游戏结束界面时间正常
        }

        // 根据需要添加进入 Loading, Playing, InDialogue 等状态的特定逻辑
        if (newStatus == GameStatus.GameOver)
        {
             Time.timeScale = 1f; // 确保游戏结束界面时间正常
             AudioManager.Instance?.PlayMusic(MusicTrack.GameOverStinger, false); // 播放游戏结束音效
        }
    }
    #endregion
}
