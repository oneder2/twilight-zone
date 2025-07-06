using UnityEngine;
using TMPro; // Use TextMeshPro / 使用 TextMeshPro
using System.Collections;
using UnityEngine.Playables; // For PlayableDirector reference if needed by events / PlayableDirector 引用（如果事件需要）
using System;

/// <summary>
/// Manages the dialogue UI panel. Handles different dialogue types:
/// - Blocking Dialogue: Pauses game (sets GameStatus.InDialogue), requires input.
/// - Notifications: Does not pause game, disappears automatically.
/// - Timeline Dialogue: Controlled by DialogueTrack, can pause Timeline.
/// Manages its own input detection when dialogue requires it.
/// 管理对话 UI 面板并处理不同类型的对话显示：
/// - 阻塞式对话：暂停游戏（设置 GameStatus.InDialogue），需要输入。
/// - 通知式对话：不暂停游戏，自动消失。
/// - Timeline 对话：由 DialogueTrack 控制，可以暂停 Timeline。
/// 在对话需要时管理自身的输入检测。
/// </summary>
public class DialogueManager : Singleton<DialogueManager>
{
    [Header("UI Elements / UI 元素")]
    [Tooltip("The main panel containing text and other UI.\n包含文本和其他 UI 的主面板。")]
    [SerializeField] private GameObject dialoguePanel;
    [Tooltip("Text component to display dialogue.\n用于显示对话的文本组件。")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [Tooltip("CanvasGroup component on the dialogue panel for fading and blocking raycasts.\n对话面板上的 CanvasGroup 组件，用于淡入淡出和阻止射线投射。")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup; // Assign this in Inspector / 在 Inspector 中分配这个

    [Header("Settings / 设置")]
    [Tooltip("Default duration for notification messages (seconds).\n通知消息的默认持续时间（秒）。")]
    [SerializeField] private float defaultNotificationDuration = 2.5f;

    // --- State / 状态 ---
    public bool IsDialogueActive { get; private set; } = false; // Is any dialogue UI visible? / 是否有任何对话 UI 可见？
    private bool isWaitingForInput = false; // Does the *current* active dialogue require input? / *当前*活动对话是否需要输入？
    private bool isTimelineControlled = false; // Was the current dialogue started by Timeline? / 当前对话是否由 Timeline 启动？
    private GameStatus statusBeforeDialogue = GameStatus.Playing; // Status before *blocking* dialogue started / *阻塞式*对话开始之前的状态
    private Coroutine currentDisplayCoroutine = null; // Handles notifications or multi-line blocking dialogue / 处理通知或多行阻塞式对话
    private string[] currentLines; // Lines for the current sequence / 当前序列的行
    private int currentLineIndex; // Index of the line currently shown / 当前显示行的索引

    // --- Timeline Integration Event / Timeline 集成事件 ---
    /// <summary>
    /// Event triggered specifically when a dialogue initiated by a PAUSABLE Timeline clip completes.
    /// 当由可暂停的 Timeline 片段发起的对话完成时专门触发的事件。
    /// </summary>
    public event Action onTimelineDialogueComplete;

    // --- Initialization / 初始化 ---
    void Start()
    {
        // Ensure CanvasGroup exists if not assigned / 如果未分配，确保 CanvasGroup 存在
        if (dialoguePanel != null && dialogueCanvasGroup == null)
        {
            dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                Debug.LogWarning("[DialogueManager] Added missing CanvasGroup component to dialoguePanel.");
            }
        }
        // Start hidden / 开始时隐藏
        HideDialoguePanel(); // Ensure it's hidden and non-blocking initially / 确保初始时隐藏且不阻挡射线
    }

    // --- Event Subscriptions (Optional) / 事件订阅（可选） ---
    // Add listeners here if needed for external dialogue triggers / 如果需要外部对话触发器，在此处添加监听器
    // void OnEnable() { /* ... subscribe ... */ }
    // void OnDisable() { StopAllCoroutines(); currentDisplayCoroutine = null; /* ... unsubscribe ... */ } // Ensure cleanup / 确保清理

    #region Public Methods for Gameplay Scripts / 用于游戏脚本的公共方法

    /// <summary>
    /// Shows dialogue that pauses the game (sets GameStatus.InDialogue) and requires player input to advance/close.
    /// 显示暂停游戏（设置 GameStatus.InDialogue）并需要玩家输入来推进/关闭的对话。
    /// </summary>
    /// <param name="lines">The lines of dialogue to display sequentially. / 要顺序显示的对话行。</param>
    public void ShowBlockingDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        StartDialogueSequence(lines, true, false); // Requires Input = true, Is Timeline = false / 需要输入 = true，是 Timeline = false
    }

    /// <summary>
    /// Shows a single line of blocking dialogue.
    /// 显示单行阻塞式对话。
    /// </summary>
    /// <param name="line">The single line of dialogue. / 单行对话。</param>
    public void ShowBlockingDialogue(string line)
    {
        ShowBlockingDialogue(new string[] { line ?? string.Empty }); // Ensure line is not null / 确保行不为 null
    }

    /// <summary>
    /// Shows a short notification message that does NOT pause the game and disappears automatically.
    /// 显示一个不暂停游戏并自动消失的短通知消息。
    /// </summary>
    /// <param name="message">The message to display. / 要显示的消息。</param>
    /// <param name="duration">How long the message stays visible (seconds). Uses default if negative. / 消息可见时长（秒）。如果为负，则使用默认值。</param>
    public void ShowNotification(string message, float duration = -1f)
    {
        if (string.IsNullOrEmpty(message)) return;
        StopCurrentDisplayCoroutine(); // Stop previous notification / 停止之前的通知
        float displayDuration = (duration >= 0) ? duration : defaultNotificationDuration;
        currentDisplayCoroutine = StartCoroutine(ShowNotificationCoroutine(message, displayDuration));
    }
    #endregion

    #region Internal Methods (Timeline & Core Logic) / 内部方法（Timeline 与核心逻辑）

    /// <summary>
    /// Shows dialogue initiated by a Timeline clip. Internal use only.
    /// 显示由 Timeline 片段发起的对话。仅供内部使用。
    /// </summary>
    /// <param name="speaker">The speaker's name (optional). / 说话者名字（可选）。</param>
    /// <param name="text">The dialogue text. / 对话文本。</param>
    /// <param name="requiresInput">If true, waits for player input. / 如果为 true，则等待玩家输入。</param>
    internal void ShowTimelineDialogue(string speaker, string text, bool requiresInput)
    {
        // Format text with speaker if provided / 如果提供了说话者，则格式化文本
        string formattedText = !string.IsNullOrEmpty(speaker) ? $"[{speaker.ToUpper()}]: {text}" : text;
        // Debug.Log($"[DialogueManager] ShowTimelineDialogue called. Text: '{text}', RequiresInput: {requiresInput}");
        StartDialogueSequence(new string[] { formattedText }, requiresInput, true); // Is Timeline = true / 是 Timeline = true
    }

    /// <summary>
    /// Starts displaying a sequence of dialogue lines with specified behavior.
    /// 开始以指定的行为显示一系列对话行。
    /// </summary>
    /// <param name="lines">Lines to display. / 要显示的行。</param>
    /// <param name="requiresInput">Does this sequence wait for input? / 此序列是否等待输入？</param>
    /// <param name="isTimeline">Was this called by Timeline? / 这是否由 Timeline 调用？</param>
    private void StartDialogueSequence(string[] lines, bool requiresInput, bool isTimeline)
    {
        // Prevent starting new blocking dialogue if already waiting for input / 如果已经在等待输入，则阻止启动新的阻塞式对话
        if (IsDialogueActive && isWaitingForInput)
        {
            // Debug.LogWarning($"[DialogueManager] Tried to start new dialogue while waiting for input. Request ignored.");
            return;
        }

        StopCurrentDisplayCoroutine(); // Stop any running notification / 停止任何正在运行的通知

        // Set up state for the new sequence / 为新序列设置状态
        currentLines = lines;
        currentLineIndex = 0;
        isTimelineControlled = isTimeline;
        IsDialogueActive = true; // Mark UI as active / 将 UI 标记为活动
        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(true)); // Notify listeners UI appeared / 通知监听器 UI 已出现

        // --- Game Status Handling / 游戏状态处理 ---
        // Only pause game for non-Timeline blocking dialogue / 仅为非 Timeline 阻塞式对话暂停游戏
        if (!isTimelineControlled && requiresInput)
        {
            statusBeforeDialogue = GameRunManager.Instance != null ? GameRunManager.Instance.CurrentStatus : GameStatus.Playing;
            if (statusBeforeDialogue == GameStatus.Playing) // Only change if currently playing / 仅当当前正在玩时才更改
            {
                GameRunManager.Instance?.ChangeGameStatus(GameStatus.InDialogue);
                // Debug.Log("[DialogueManager] Game Status changed to InDialogue.");
            }
        }
        // --- End Game Status Handling / 结束游戏状态处理 ---

        // --- Ensure Panel is Active and Visible / 确保面板是活动且可见的 ---
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        else { Debug.LogError("[DialogueManager] Dialogue Panel reference is missing!"); return; }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f; // Set alpha to 1 when showing / 显示时将 alpha 设置为 1
            dialogueCanvasGroup.blocksRaycasts = true; // Block raycasts when active / 活动时阻止射线投射
        }
        // --- End Ensure Panel Visible / 结束确保面板可见 ---

        DisplayCurrentLine(requiresInput); // Display the first line / 显示第一行
    }

    /// <summary>
    /// Displays the current line and sets the input waiting state.
    /// 显示当前行并设置输入等待状态。
    /// </summary>
    /// <param name="waitForInputFlag">Should wait for input after displaying? / 显示后是否应等待输入？</param>
    private void DisplayCurrentLine(bool waitForInputFlag)
    {
        // Check if lines are valid / 检查行是否有效
        if (currentLines == null || currentLineIndex >= currentLines.Length)
        {
            HideDialogue(); // Safety check: hide if no more lines / 安全检查：如果没有更多行则隐藏
            return;
        }
        // Update the text component / 更新文本组件
        if (dialogueText != null) {
             dialogueText.text = currentLines[currentLineIndex];
        } else { Debug.LogError("[DialogueManager] dialogueText reference is null!"); }
        // Set whether we need player input to proceed / 设置是否需要玩家输入才能继续
        isWaitingForInput = waitForInputFlag;
        // Debug.Log($"[DialogueManager] Displaying line {currentLineIndex + 1}/{currentLines.Length}. Waiting for input: {isWaitingForInput}");
    }

    /// <summary>
    /// Coroutine for displaying notification messages.
    /// 用于显示通知消息的协程。
    /// </summary>
    private IEnumerator ShowNotificationCoroutine(string message, float duration)
    {
        IsDialogueActive = true; // Mark UI active / 标记 UI 活动
        isWaitingForInput = false; // Notifications don't wait / 通知不等待
        isTimelineControlled = false;
        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(true)); // Notify UI appeared / 通知 UI 已出现

        if (dialogueText != null) dialogueText.text = message;

        // Ensure Panel is Active and Visible / 确保面板是活动且可见的
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f; // Set alpha to 1 / 将 alpha 设置为 1
            // Decide if notifications should block clicks / 决定通知是否应阻止点击
            dialogueCanvasGroup.blocksRaycasts = true; // Or false if they shouldn't block / 如果不应阻止则为 false
        }

        yield return new WaitForSeconds(duration); // Wait for specified duration / 等待指定时长

        // Check if this coroutine was stopped/replaced before hiding / 检查此协程在隐藏之前是否已停止/替换
        if (currentDisplayCoroutine == null) yield break; // Another coroutine took over / 另一个协程接管了

        HideDialoguePanel(); // Use helper to hide and set raycasts / 使用辅助方法隐藏并设置射线投射
        IsDialogueActive = false; // Mark UI inactive / 标记 UI 非活动
        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(false)); // Notify UI disappeared / 通知 UI 已消失
        currentDisplayCoroutine = null; // Clear coroutine reference / 清除协程引用
    }

    /// <summary>
    /// Stops the currently active display coroutine (used for notifications).
    /// 停止当前活动的显示协程（用于通知）。
    /// </summary>
    private void StopCurrentDisplayCoroutine()
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }
    }

    /// <summary>
    /// Handles player input during dialogue, INDEPENDENTLY of Player script's input disabling.
    /// 在对话期间处理玩家输入，独立于 Player 脚本的输入禁用。
    /// </summary>
    void Update()
    {
        // Process input ONLY if the dialogue panel is active AND waiting for input.
        // 仅当对话面板激活并等待输入时处理输入。
        if (IsDialogueActive && isWaitingForInput)
        {
            // Escape key always hides/skips the current dialogue sequence
            // Escape 键始终隐藏/跳过当前对话序列
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Debug.Log("[DialogueManager] Dialogue skipped by player (Escape pressed).");
                HideDialogue(); // Handles cleanup and state restoration / 处理清理和状态恢复
                return; // Important: return after hiding / 重要：隐藏后返回
            }

            // Standard progression keys (Space, Enter, Left Mouse Button)
            // 标准推进键（空格、回车、鼠标左键）
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                // Debug.Log("[DialogueManager] Player proceeding to next line.");
                ProceedToNextLine();
            }
        }
        // Optional: Allow clicking to dismiss notifications early / 可选：允许点击以提前关闭通知
        else if (IsDialogueActive && !isWaitingForInput && !isTimelineControlled && currentDisplayCoroutine != null) // It's a notification / 这是一个通知
        {
             if (Input.GetMouseButtonDown(0)) // If player clicks during notification / 如果玩家在通知期间点击
             {
                  // Debug.Log("[DialogueManager] Notification dismissed early by click.");
                  StopCurrentDisplayCoroutine(); // Stop the timer / 停止计时器
                  HideDialoguePanel(); // Hide the panel / 隐藏面板
                  IsDialogueActive = false; // Mark inactive / 标记为非活动
                  EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(false)); // Notify hidden / 通知已隐藏
             }
        }
    }

    /// <summary>
    /// Advances to the next line or finishes the dialogue sequence.
    /// 前进到下一行或完成对话序列。
    /// </summary>
    private void ProceedToNextLine()
    {
        currentLineIndex++; // Move to next line index / 移动到下一行索引
        // Check if there are more lines / 检查是否还有更多行
        if (currentLines != null && currentLineIndex < currentLines.Length)
        {
            DisplayCurrentLine(true); // Display next line, assume it requires input / 显示下一行，假设它需要输入
        }
        else
        {
            // This was the last line / 这是最后一行
            // Debug.Log("[DialogueManager] Last line processed.");
            HideDialogue(); // Finish the sequence / 完成序列
        }
    }

    /// <summary>
    /// Hides the dialogue UI and performs necessary cleanup and state restoration.
    /// 隐藏对话 UI 并执行必要的清理和状态恢复。
    /// </summary>
    public void HideDialogue()
    {
        // Only run if dialogue is actually active / 仅在对话实际激活时运行
        if (!IsDialogueActive) return;

        // Debug.Log("[DialogueManager] HideDialogue called.");
        StopCurrentDisplayCoroutine(); // Stop notification timer if active / 如果活动则停止通知计时器
        HideDialoguePanel(); // Use helper to hide and set raycasts / 使用辅助方法隐藏并设置射线投射

        bool wasTimeline = isTimelineControlled; // Store state before resetting / 在重置之前存储状态

        // Reset state flags / 重置状态标志
        IsDialogueActive = false;
        isWaitingForInput = false;
        isTimelineControlled = false;
        currentLines = null;
        currentLineIndex = 0;

        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(false)); // Notify UI hidden / 通知 UI 已隐藏

        // --- Restore Game Status / 恢复游戏状态 ---
        // Only restore if it was a blocking dialogue that changed the status
        // 仅当是改变状态的阻塞式对话时才恢复
        if (!wasTimeline && statusBeforeDialogue == GameStatus.Playing)
        {
            if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.InDialogue)
            {
                GameRunManager.Instance.ChangeGameStatus(GameStatus.Playing);
                // Debug.Log("[DialogueManager] Game Status restored to Playing.");
            }
        }
        // --- End Restore Game Status / 结束恢复游戏状态 ---


        // --- Trigger Timeline Completion / 触发 Timeline 完成 ---
        // If this dialogue was controlled by a pausable Timeline clip, notify it.
        // 如果此对话由可暂停的 Timeline 片段控制，请通知它。
        if (wasTimeline && onTimelineDialogueComplete != null)
        {
            // Debug.Log("[DialogueManager] Invoking onTimelineDialogueComplete.");
            onTimelineDialogueComplete?.Invoke();
            // The DialoguePlayableBehaviour should handle unsubscribing / DialoguePlayableBehaviour 应处理取消订阅
        }
        // --- End Trigger Timeline Completion / 结束触发 Timeline 完成 ---
    }

    /// <summary>
    /// Helper to hide the panel UI and disable raycasts.
    /// 仅隐藏面板 UI 并禁用射线投射的辅助方法。
    /// </summary>
    private void HideDialoguePanel()
    {
        // Set alpha to 0 and disable raycasts via CanvasGroup / 通过 CanvasGroup 将 alpha 设置为 0 并禁用射线投射
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.blocksRaycasts = false;
        }
        // Also deactivate the panel GameObject itself for performance / 同时停用面板 GameObject 本身以提高性能
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // Ensure cleanup on destroy / 确保在销毁时清理
    void OnDestroy()
    {
        StopAllCoroutines(); // Stop any running coroutines / 停止任何正在运行的协程
        // Unsubscribe from events if done in OnEnable / 如果在 OnEnable 中完成，则取消订阅事件
    }
    #endregion
}
