using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement; // Reference the Scene Management namespace


// Recommended: More detailed game status enum
public enum GameStatus { 
    InMenu, 
    Loading, 
    Playing, 
    Paused, 
    InCutscene, 
    GameOver, 
    InDialogue 
    }


// GameStatus enum remains the same
// public enum GameStatus { InMenu, Loading, Playing, Paused, InCutscene, GameOver, InDialogue }

/// <summary>
/// Manages the overall game running state and handles global operations like pausing and skipping timelines.
/// Now relies on Signals from Timeline to enter/exit cutscene state.
/// 管理全局游戏运行状态，处理暂停和跳过时间轴等全局操作。
/// 现在依赖 Timeline 的 Signals 来进入/退出过场动画状态。
/// </summary>
public class GameRunManager : Singleton<GameRunManager>
{
    [Header("State Management")]
    public GameStatus CurrentStatus { get; private set; } = GameStatus.InMenu;
    public bool IsPaused => CurrentStatus == GameStatus.Paused;
    public bool IsCutsceneActive => CurrentStatus == GameStatus.InCutscene;

    [Header("Timeline Settings")]
    [Tooltip("Reference to the main PlayableDirector used for cutscenes (if centralized). Can be null if directors are managed per-trigger.")]
    [SerializeField] private PlayableDirector mainCutsceneDirector; // Optional: Assign if you have one main director // 可选：如果有一个主 Director，在此分配

    // --- Removed CutscenePlayer registration logic ---
    // private CutscenePlayer activeCutscenePlayer = null; // REMOVED // 已移除

    protected override void Awake()
    {
        base.Awake();
        // Ensure this manager persists across scenes if it's Application-Level
        // DontDestroyOnLoad(gameObject); // Make sure your Singleton base handles this or add it here // 确保 Singleton 基类处理此问题或在此处添加

        CurrentStatus = GameStatus.InMenu; // Assuming game starts at the main menu // 假设游戏从主菜单开始
        AudioManager.Instance?.PlayMusic(MusicTrack.MainMenuTheme); // Ensure AudioManager exists // 确保 AudioManager 存在
    }

    void Update()
    {
        // Handle global timeline skipping input
        // 处理全局时间轴跳过输入
        HandleTimelineSkipInput();
    }

    /// <summary>
    /// Changes the detailed game status and triggers events.
    /// Now also callable directly or via Signal Receivers.
    /// 更改详细的游戏状态并触发事件。
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

    // --- New Methods Callable by Timeline Signals ---
    // --- 可由 Timeline Signals 调用的新方法 ---

    /// <summary>
    /// Public method to be called by a Signal Receiver when a Timeline starts.
    /// 当 Timeline 开始时，由 Signal Receiver 调用的公共方法。
    /// </summary>
    public void EnterCutsceneState()
    {
        Debug.Log("[GameRunManager] Signal received: Entering Cutscene State.");
        ChangeGameStatus(GameStatus.InCutscene);
    }

    /// <summary>
    /// Public method to be called by a Signal Receiver when a Timeline ends normally.
    /// 当 Timeline 正常结束时，由 Signal Receiver 调用的公共方法。
    /// </summary>
    public void ExitCutsceneState()
    {
        // Only exit if we are actually in the cutscene state (prevents issues if called incorrectly)
        // 仅当实际处于过场动画状态时才退出（防止调用不正确导致的问题）
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
    // These methods remain largely the same, but ensure they handle the InCutscene state appropriately.
    // 这些方法基本保持不变，但要确保它们能适当地处理 InCutscene 状态。

    public void StartGameSession()
    {
        // Ensure we are not starting while in a cutscene (shouldn't happen from menu)
        // 确保不在过场动画期间开始（不应从菜单发生）
        if (CurrentStatus == GameStatus.InMenu || CurrentStatus == GameStatus.GameOver)
        {
            ChangeGameStatus(GameStatus.Playing); // Or Loading first if needed // 或者如果需要，先 Loading
            Debug.Log("StartGameSession called, game start.");
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
        if (CurrentStatus == GameStatus.Paused)
        {
            // Determine what state to return to (Playing or InCutscene?)
            // This needs careful handling. Simplest is to always return to Playing,
            // but that might break cutscene flow.
            // A better approach might be to store the pre-pause state.
            // For now, let's assume resuming always goes back to Playing.
            // If a Timeline was paused, it should resume automatically if Time.timeScale goes back to 1.
            // 确定要返回哪个状态（Playing 还是 InCutscene？）
            // 这需要仔细处理。最简单的方法是总是返回 Playing，但这可能会破坏过场动画流程。
            // 更好的方法可能是存储暂停前的状态。
            // 目前，我们假设恢复总是回到 Playing。
            // 如果 Timeline 被暂停，当 Time.timeScale 恢复为 1 时，它应该自动恢复。
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

        // If a cutscene (Timeline) was active, stop it.
        // 如果过场动画（Timeline）处于活动状态，则停止它。
        if (IsCutsceneActive)
        {
             Debug.LogWarning("Ending game session while Timeline was active. Stopping active director.");
             // Find and stop the active director - Requires a way to find it!
             // 查找并停止活动的 director - 需要一种方法来找到它！
             PlayableDirector activeDirector = FindActiveDirector(); // Implement FindActiveDirector() // 实现 FindActiveDirector()
             if (activeDirector != null && activeDirector.state == PlayState.Playing)
             {
                 activeDirector.Stop();
             }
        }

        AudioManager.Instance?.PlayMusic(MusicTrack.MainMenuTheme);
        ChangeGameStatus(GameStatus.InMenu);
        SessionStateManager.Instance?.ClearAllSceneStates();
        Time.timeScale = 1f; // Ensure time scale is reset // 确保时间缩放已重置

        // Scene loading/unloading is handled externally (e.g., ReturnToMenuHandler)
        // 场景加载/卸载由外部处理（例如 ReturnToMenuHandler）
    }


    // --- Timeline Skipping Logic ---
    // --- 时间轴跳过逻辑 ---

    private void HandleTimelineSkipInput()
    {
        // Only allow skipping if a cutscene (Timeline) is active
        // 仅当过场动画（Timeline）处于活动状态时才允许跳过
        if (CurrentStatus == GameStatus.InCutscene)
        {
            // Use your preferred skip input (e.g., Escape, Space, specific button)
            // 使用你偏好的跳过输入（例如 Escape、Space、特定按钮）
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Skip input detected by GameRunManager.");
                SkipCurrentTimeline();
            }
        }
    }

    /// <summary>
    /// Attempts to skip the currently playing Timeline.
    /// 尝试跳过当前正在播放的 Timeline。
    /// </summary>
    public void SkipCurrentTimeline()
    {
        if (CurrentStatus != GameStatus.InCutscene)
        {
            Debug.LogWarning("SkipCurrentTimeline called, but game status is not InCutscene.");
            return;
        }

        // Find the currently playing PlayableDirector.
        // This is the tricky part. You might need a way to register the active director
        // when it starts playing, or find it dynamically. Finding dynamically can be slow.
        // 查找当前正在播放的 PlayableDirector。
        // 这是棘手的部分。你可能需要在活动 director 开始播放时注册它，或者动态查找它。动态查找可能很慢。
        PlayableDirector activeDirector = FindActiveDirector(); // Implement this helper method // 实现此辅助方法

        if (activeDirector != null && activeDirector.state == PlayState.Playing && activeDirector.playableAsset != null)
        {
            Debug.Log($"GameRunManager requesting skip for director playing: {activeDirector.playableAsset.name}");

            // --- Option 1: Jump to End --- // --- 选项 1：跳转到结尾 ---
            // This relies on signals at the very end of the timeline to set the final state.
            // 这依赖于时间轴最末尾的信号来设置最终状态。
            activeDirector.time = activeDirector.duration;
            // Ensure the director updates immediately to process the end state/signals
            // 确保 director 立即更新以处理结束状态/信号
            activeDirector.Evaluate();

            // --- Option 2: Stop and Manually Trigger Final State (More complex) --- // --- 选项 2：停止并手动触发最终状态（更复杂）---
            // activeDirector.Stop();
            // Manually call methods that set the final scene/player state, potentially
            // using data associated with the timeline asset. This is closer to the old ApplySkipState.
            // 手动调用设置最终场景/玩家状态的方法，可能使用与时间轴资产关联的数据。这更接近旧的 ApplySkipState。

            // --- Option 3: Use a dedicated "Skip Signal" --- // --- 选项 3：使用专用的“跳过信号”---
            // Place a signal near the end of your timelines specifically for skip actions.
            // When skip is pressed, jump time just before this signal:
            // activeDirector.time = skipSignalTime - 0.1f; // Need to know skipSignalTime
            // activeDirector.Evaluate();
            // 在你的时间轴末尾附近放置一个专门用于跳过操作的信号。
            // 按下跳过时，将时间跳转到此信号之前：
            // activeDirector.time = skipSignalTime - 0.1f; // 需要知道 skipSignalTime
            // activeDirector.Evaluate();

            // For simplicity, let's assume Option 1 (jumping to end) and relying on end-timeline signals.
            // 为简单起见，我们假设使用选项 1（跳转到结尾）并依赖时间轴末尾的信号。

            // Force the state back to Playing immediately after attempting the skip.
            // The ExitCutsceneState signal at the end of the timeline might also fire,
            // but calling ChangeGameStatus here ensures responsiveness.
            // 尝试跳过后立即强制状态返回 Playing。
            // 时间轴末尾的 ExitCutsceneState 信号也可能触发，但在此处调用 ChangeGameStatus 可确保响应性。
            ChangeGameStatus(GameStatus.Playing);
        }
        else
        {
            Debug.LogWarning("Skip requested, but no active PlayableDirector found or it's not playing. Forcing state back to Playing.");
            ChangeGameStatus(GameStatus.Playing); // Force state back if something is wrong // 如果出现问题，强制状态返回
        }
    }

    /// <summary>
    /// Helper method to find the currently active PlayableDirector.
    /// IMPLEMENTATION NEEDED: This needs a robust way to find the director.
    /// Options:
    /// 1. Maintain a reference: When a director starts, it registers itself here.
    /// 2. Find by Tag/Component: Less efficient, especially with many directors.
    /// 3. Assume a single main director: Use 'mainCutsceneDirector' if assigned.
    /// 查找当前活动的 PlayableDirector 的辅助方法。
    /// 需要实现：这需要一种可靠的方法来查找 director。
    /// 选项：
    /// 1. 维护引用：当 director 启动时，在此处注册自己。
    /// 2. 按标签/组件查找：效率较低，尤其是在有许多 director 的情况下。
    /// 3. 假设只有一个主 director：如果已分配，则使用 'mainCutsceneDirector'。
    /// </summary>
    private PlayableDirector FindActiveDirector()
    {
        // Example Implementation (Simple, assumes one active director):
        // This is inefficient if called frequently. Consider registration.
        // 示例实现（简单，假设只有一个活动的 director）：
        // 如果频繁调用，效率会很低。考虑注册。
        var directors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
        foreach (var director in directors)
        {
            // Check if it's playing and potentially has a cutscene-specific tag?
            // 检查它是否正在播放，并且可能具有特定于过场动画的标签？
            if (director.state == PlayState.Playing && director.playableAsset != null)
            {
                // Add more checks if needed (e.g., check a tag, check if it's the 'main' one)
                // 如果需要，添加更多检查（例如，检查标签，检查是否是“主”标签）
                return director;
            }
        }

        // Fallback to main director if assigned and playing
        // 如果已分配并正在播放，则回退到主 director
        if (mainCutsceneDirector != null && mainCutsceneDirector.state == PlayState.Playing)
        {
            return mainCutsceneDirector;
        }

        Debug.LogWarning("FindActiveDirector: Could not find an active PlayableDirector.");
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
        // Ensure time scale is reset when exiting pause or entering normally-timed states
        // 确保在退出暂停或进入正常时间状态时重置时间缩放
        else if (oldStatus == GameStatus.Paused || newStatus == GameStatus.Playing || newStatus == GameStatus.InCutscene || newStatus == GameStatus.InMenu)
        {
             // Restore time scale only if it was paused
             // 仅当暂停时才恢复时间缩放
             if (oldStatus == GameStatus.Paused && Time.timeScale != 1f)
             {
                  Time.timeScale = 1f;
             }
             // Or ensure it's 1 if entering a non-paused state (safety check)
             // 或者确保在进入非暂停状态时为 1（安全检查）
             else if (newStatus != GameStatus.Paused && Time.timeScale != 1f)
             {
                 Time.timeScale = 1f;
             }
        }

        // Handle specific state entry/exit logic if needed
        // 如果需要，处理特定的状态进入/退出逻辑
        if (newStatus == GameStatus.InMenu)
        {
            // Logic already handled in EndGameSession, but could add more here if needed
            // 逻辑已在 EndGameSession 中处理，但如果需要，可以在此处添加更多逻辑
        }

        if (newStatus == GameStatus.GameOver)
        {
             Time.timeScale = 1f; // Ensure time isn't frozen on game over screen // 确保时间在游戏结束屏幕上不会冻结
             // Potentially trigger game over UI, sounds etc.
             // 可能触发游戏结束 UI、声音等。
             AudioManager.Instance?.PlayMusic(MusicTrack.GameOverStinger, false); // Play game over sound // 播放游戏结束声音
        }
    }
}
