// File: Scripts/Manager/GlobalStatus/GameRunManager.cs
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement; // Reference the Scene Management namespace // 引用场景管理命名空间


// Recommended: More detailed game status enum
// 推荐：更详细的游戏状态枚举
public enum GameStatus {
    InMenu,
    Loading, // Added state for transitions // 为转换添加的状态
    Playing,
    Paused,
    InCutscene,
    GameOver,
    InDialogue
    }


/// <summary>
/// Manages the overall game running state and handles global operations like pausing and skipping timelines.
/// 管理全局游戏运行状态，处理暂停和跳过时间轴等全局操作。
/// Now relies on Signals from Timeline to enter/exit cutscene state.
/// 现在依赖 Timeline 的 Signals 来进入/退出过场动画状态。
/// </summary>
public class GameRunManager : Singleton<GameRunManager>
{
    [Header("State Management")]
    public GameStatus CurrentStatus { get; private set; } = GameStatus.InMenu;
    public bool IsPaused => CurrentStatus == GameStatus.Paused;
    public bool IsCutsceneActive => CurrentStatus == GameStatus.InCutscene;

    // --- NEW: Loading Lock Flag ---
    // --- 新增：加载锁标志 ---
    [Header("Scene Loading Status")]
    [Tooltip("Indicates if a scene loading or unloading process is currently active.")]
    // [Tooltip("指示场景加载或卸载过程当前是否处于活动状态。")]
    public bool IsSceneLoadingOrUnloading { get; private set; } = false;
    // --- End New Flag ---


    [Header("Timeline Settings")]
    [Tooltip("Reference to the main PlayableDirector used for cutscenes (if centralized). Can be null if directors are managed per-trigger.")]
    // [Tooltip("对用于过场动画的主 PlayableDirector 的引用（如果是集中的）。如果 director 是按触发器管理的，则可以为 null。")]
    [SerializeField] private PlayableDirector mainCutsceneDirector; // Optional: Assign if you have one main director // 可选：如果有一个主 Director，在此分配

    public static bool InitiateRestartFlow = false; // 标志是否需要自动重启 (Flag indicating if auto-restart is needed)


    protected override void Awake()
    {
        base.Awake();
        // Ensure this manager persists across scenes if it's Application-Level
        // 如果是应用程序级别，请确保此管理器跨场景持久存在
        // DontDestroyOnLoad(gameObject); // Make sure your Singleton base handles this or add it here // 确保 Singleton 基类处理此问题或在此处添加

        CurrentStatus = GameStatus.InMenu; // Assuming game starts at the main menu // 假设游戏从主菜单开始
        IsSceneLoadingOrUnloading = false; // Ensure flag is reset on awake // 确保标志在 awake 时重置
        // AudioManager.Instance?.PlayMusic(MusicTrack.MainMenuTheme); // Play music AFTER ensuring instance exists // 确保实例存在后播放音乐
    }

     void Start()
     {
         // Play initial music here if needed, after all Awakes are done
         // 如果需要，在此处播放初始音乐，在所有 Awake 完成后
         if (CurrentStatus == GameStatus.InMenu)
         {
              AudioManager.Instance?.PlayMusic(MusicTrack.MainMenuTheme);
         }
     }


    void Update()
    {
        // Handle global timeline skipping input
        // 处理全局时间轴跳过输入
        HandleTimelineSkipInput();
    }

    // --- NEW: Method to control the loading lock ---
    // --- 新增：控制加载锁的方法 ---
    /// <summary>
    /// Sets the scene loading/unloading status flag. Should be called by scene transition managers.
    /// 设置场景加载/卸载状态标志。应由场景转换管理器调用。
    /// </summary>
    /// <param name="isLoading">True if loading/unloading has started, false when completed. / 如果加载/卸载已开始，则为 true；完成后为 false。</param>
    public void SetLoadingStatus(bool isLoading)
    {
        IsSceneLoadingOrUnloading = isLoading;
        Debug.Log($"[GameRunManager] IsSceneLoadingOrUnloading set to: {isLoading} (Current Status: {CurrentStatus})");
    }
    // --- End New Method ---


    /// <summary>
    /// Changes the detailed game status and triggers events.
    /// 更改详细的游戏状态并触发事件。
    /// Now also callable directly or via Signal Receivers.
    /// 现在也可以直接调用或通过 Signal Receiver 调用。
    /// </summary>
    /// <param name="newStatus">The new status to transition to. // 要转换到的新状态。</param>
    public void ChangeGameStatus(GameStatus newStatus)
    {
        if (CurrentStatus == newStatus) return; // No change // 无变化

        GameStatus previousStatus = CurrentStatus;
        CurrentStatus = newStatus;
        Debug.Log($"[GameRunManager] Game Status Changed: {previousStatus} -> {CurrentStatus}");

        HandleStatusChange(previousStatus, newStatus);
        EventManager.Instance?.TriggerEvent(new GameStatusChangedEvent(CurrentStatus, previousStatus));
    }

    // --- Methods Callable by Timeline Signals ---
    // --- 可由 Timeline Signals 调用的方法 ---
    public void EnterCutsceneState()
    {
        Debug.Log("[GameRunManager] Signal received: Entering Cutscene State.");
        ChangeGameStatus(GameStatus.InCutscene);
    }
    public void ExitCutsceneState()
    {
        if (CurrentStatus == GameStatus.InCutscene)
        {
            Debug.Log("[GameRunManager] Signal received: Exiting Cutscene State.");
            ChangeGameStatus(GameStatus.Playing);
        }
        else
        {
            Debug.LogWarning($"[GameRunManager] ExitCutsceneState called, but current status is {CurrentStatus}. No state change.");
        }
    }

    // --- Existing State Management Methods (Pause, Resume, Start/End Session) ---
    // --- 现有的状态管理方法（暂停、恢复、开始/结束会话）---
    public void StartGameSession()
    {
        // Called by StartGameManager *before* loading starts
        // 由 StartGameManager 在加载开始*之前*调用
        if (CurrentStatus == GameStatus.InMenu || CurrentStatus == GameStatus.GameOver)
        {
            // ChangeGameStatus(GameStatus.Loading); // Set to Loading state // 设置为 Loading 状态
            Debug.Log("[GameRunManager] StartGameSession called, preparing for game start.");
            // Reset relevant session data if needed
            // 如果需要，重置相关的会话数据
            // SessionStateManager.Instance?.ClearAllSceneStates(); // Example: Clear previous session states // 示例：清除之前的会话状态
            EventManager.Instance?.ResetTimeEvents(); // Example: Reset timed events // 示例：重置定时事件
        }
        else
        {
             Debug.LogWarning($"StartGameSession called but current status is {CurrentStatus}.");
        }
    }

    public void PauseGame()
    {
        // Allow pausing during gameplay or cutscenes (if desired)
        // 允许在游戏或过场动画期间暂停（如果需要）
         if (IsSceneLoadingOrUnloading) { Debug.LogWarning("[GameRunManager] Cannot pause while loading/unloading scenes."); return; } // Prevent pausing during load // 防止加载期间暂停

        if (CurrentStatus == GameStatus.Playing || CurrentStatus == GameStatus.InCutscene)
        {
            ChangeGameStatus(GameStatus.Paused);
        }
        else
        {
             Debug.LogWarning($"PauseGame called but current status is {CurrentStatus}.");
        }
    }

    public void ResumeGame()
    {
         if (IsSceneLoadingOrUnloading) { Debug.LogWarning("[GameRunManager] Cannot resume while loading/unloading scenes."); return; } // Prevent resuming during load // 防止加载期间恢复

        if (CurrentStatus == GameStatus.Paused)
        {
            // Simplest: always return to Playing. If a Timeline was paused, it should resume.
            // 最简单：总是返回 Playing。如果 Timeline 被暂停，它应该会恢复。
            ChangeGameStatus(GameStatus.Playing);
        }
         else
        {
             Debug.LogWarning($"ResumeGame called but current status is {CurrentStatus}, not Paused.");
        }
    }

    public void EndGameSession()
    {
        Debug.Log("[GameRunManager] Ending Game Session...");

        if (IsCutsceneActive)
        {
             Debug.LogWarning("Ending game session while Timeline was active. Stopping active director.");
             PlayableDirector activeDirector = FindActiveDirector();
             if (activeDirector != null && activeDirector.state == PlayState.Playing)
             {
                 activeDirector.Stop();
             }
        }

        // --- Music Change ---
        if (AudioManager.Instance != null) {
             Debug.Log("[GameRunManager] Stopping current music and playing Main Menu theme.");
             AudioManager.Instance.StopMusic(false); // Stop immediately // 立即停止
             AudioManager.Instance.PlayMusic(MusicTrack.MainMenuTheme); // Play menu theme // 播放菜单主题
        } else { Debug.LogWarning("[GameRunManager] AudioManager not found during EndGameSession."); }
        // --- End Music Change ---

        ChangeGameStatus(GameStatus.InMenu); // Set status to InMenu // 将状态设置为 InMenu
        SessionStateManager.Instance?.ClearAllSceneStates(); // Ensure session state is cleared // 确保会话状态已清除
        Time.timeScale = 1f; // Ensure time scale is reset // 确保时间缩放已重置

        // Scene loading/unloading is handled externally (e.g., EndGameManager)
        // 场景加载/卸载由外部处理（例如 EndGameManager）
    }


    // --- Timeline Skipping Logic ---
    // --- 时间轴跳过逻辑 ---
    private void HandleTimelineSkipInput()
    {
        if (CurrentStatus == GameStatus.InCutscene && !IsSceneLoadingOrUnloading) // Don't skip if loading // 加载时不跳过
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Skip input detected by GameRunManager.");
                SkipCurrentTimeline();
            }
        }
    }
    public void SkipCurrentTimeline()
    {
        if (CurrentStatus != GameStatus.InCutscene) return;
        PlayableDirector activeDirector = FindActiveDirector();
        if (activeDirector != null && activeDirector.state == PlayState.Playing && activeDirector.playableAsset != null)
        {
            Debug.Log($"GameRunManager requesting skip for director playing: {activeDirector.playableAsset.name}");
            activeDirector.time = activeDirector.duration;
            activeDirector.Evaluate();
            ChangeGameStatus(GameStatus.Playing); // Force state back // 强制状态返回
        }
        else
        {
            Debug.LogWarning("Skip requested, but no active PlayableDirector found or it's not playing. Forcing state back to Playing.");
            ChangeGameStatus(GameStatus.Playing); // Force state back if something is wrong // 如果出现问题，强制状态返回
        }
    }
    private PlayableDirector FindActiveDirector()
    {
        var directors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
        foreach (var director in directors) { if (director.state == PlayState.Playing && director.playableAsset != null) return director; }
        if (mainCutsceneDirector != null && mainCutsceneDirector.state == PlayState.Playing) return mainCutsceneDirector;
        // Debug.LogWarning("FindActiveDirector: Could not find an active PlayableDirector."); // Less verbose // 减少冗余
        return null;
    }


    // --- Internal State Handling ---
    // --- 内部状态处理 ---
    private void HandleStatusChange(GameStatus oldStatus, GameStatus newStatus)
    {
        // Handle Time Scale // 处理时间缩放
        if (newStatus == GameStatus.Paused)
        {
            Time.timeScale = 0f;
        }
        else if (oldStatus == GameStatus.Paused && newStatus != GameStatus.Paused) // Resuming from pause // 从暂停恢复
        {
            Time.timeScale = 1f;
        }
         // Ensure time scale is 1 if entering a non-paused, non-loading state
         // 确保如果进入非暂停、非加载状态，时间缩放为 1
         else if (newStatus != GameStatus.Paused && newStatus != GameStatus.Loading && Time.timeScale != 1f)
         {
             Debug.LogWarning($"[GameRunManager] Time scale was {Time.timeScale}, resetting to 1 for status {newStatus}");
             Time.timeScale = 1f;
         }


        // Handle specific state entry/exit logic if needed
        // 如果需要，处理特定的状态进入/退出逻辑
        if (newStatus == GameStatus.GameOver)
        {
             Time.timeScale = 1f; // Ensure time isn't frozen on game over screen // 确保时间在游戏结束屏幕上不会冻结
             AudioManager.Instance?.PlayMusic(MusicTrack.GameOverStinger, false); // Play game over sound // 播放游戏结束声音
        }
        // Add more specific logic here if needed for entering Loading, Playing etc.
        // 如果需要，在此处为进入 Loading、Playing 等添加更具体的逻辑。
    }
}
