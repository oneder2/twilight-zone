using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Playables;
using System;
// using YourEventsNamespace; // Make sure your EventManager related using statements are correct

/// <summary>
/// Manages the dialogue UI panel and handles displaying text.
/// 管理对话 UI 面板并处理文本显示。
/// Aware of the global GameStatus to avoid interfering with InCutscene state.
/// 能感知全局 GameStatus 以避免干扰 InCutscene 状态。
/// Modified for Timeline integration.
/// 为 Timeline 集成进行了修改。
/// </summary>
public class DialogueManager : Singleton<DialogueManager> // Ensure Singleton<T> is correctly implemented
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float displayTimePerLine = 3f; // Used only for auto-advancing single lines (not via Timeline)

    public bool IsDialogueActive { get; private set; } = false;

    // --- Timeline Integration ---
    /// <summary>
    /// Event triggered specifically when a dialogue initiated by Timeline completes.
    /// 当由 Timeline 发起的对话完成时专门触发的事件。
    /// </summary>
    public event Action onTimelineDialogueComplete;
    // --- End Timeline Integration ---


    private Coroutine currentDialogueCoroutine = null;
    private string[] currentLines;
    private int currentLineIndex;
    private bool waitingForInput = false;
    // private bool isCurrentDialoguePausable = false; // This seems less relevant for Timeline control, which handles pausing itself

    private GameStatus statusBeforeDialogue = GameStatus.InMenu; // Default value


    // --- EventManager Subscriptions (Keep as is if still needed for other systems) ---
    void OnEnable()
    {
        // Keep your existing EventManager listeners if other parts of your game use them
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<ShowDialogueRequestedEvent>(HandleShowDialogueRequest);
            EventManager.Instance.AddListener<ShowPausableDialogueRequestedEvent>(HandleShowPausableDialogueRequest);
            Debug.Log("[DialogueManager] Subscribed to dialogue request events.");
        }
        else { Debug.LogError("[DialogueManager] EventManager.Instance is null on Enable."); }
    }

    void OnDisable()
    {
        // Keep your existing EventManager listeners if other parts of your game use them
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<ShowDialogueRequestedEvent>(HandleShowDialogueRequest);
            EventManager.Instance.RemoveListener<ShowPausableDialogueRequestedEvent>(HandleShowPausableDialogueRequest);
            Debug.Log("[DialogueManager] Unsubscribed from dialogue request events.");
        }
        // Ensure coroutines are stopped and state is reset on disable
        StopExistingDialogue();
        if(IsDialogueActive) HideDialogue(); // Reset state if disabled mid-dialogue
    }

    // --- Event Handlers (Keep as is) ---
    private void HandleShowDialogueRequest(ShowDialogueRequestedEvent eventData)
    {
        if (eventData == null || eventData.Lines == null || eventData.Lines.Length == 0) return;
        Debug.Log($"[DialogueManager] Received ShowDialogueRequestedEvent. Showing normal dialogue.");
        // isCurrentDialoguePausable = false; // Not directly used in the modified logic here
        ShowDialogueInternal(eventData.Lines, false); // Assume non-pausable by default from events
    }

    private void HandleShowPausableDialogueRequest(ShowPausableDialogueRequestedEvent eventData)
    {
        if (eventData == null || eventData.Lines == null || eventData.Lines.Length == 0 /*|| eventData.DirectorToPause == null*/) return; // DirectorToPause might not be needed here anymore
        Debug.Log($"[DialogueManager] Received ShowPausableDialogueRequestedEvent. Showing pausable dialogue.");
        // isCurrentDialoguePausable = true; // Not directly used in the modified logic here
        ShowDialogueInternal(eventData.Lines, true); // Mark as requiring input
    }

    // --- Internal Display Logic (Modified slightly) ---
    private void ShowDialogueInternal(string[] lines, bool requiresInput)
    {
        StopExistingDialogue(); // Stop any previous dialogue first

        // Record the current status before potentially changing it
        statusBeforeDialogue = GameRunManager.Instance != null ? GameRunManager.Instance.CurrentStatus : GameStatus.Playing; // Fallback
        Debug.Log($"[DialogueManager] Starting dialogue. Status before: {statusBeforeDialogue}");

        IsDialogueActive = true;
        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(true)); // Notify listeners dialogue started

        // Change game status ONLY if starting from 'Playing' state
        if (statusBeforeDialogue == GameStatus.Playing)
        {
            GameRunManager.Instance?.ChangeGameStatus(GameStatus.InDialogue);
            Debug.Log("[DialogueManager] Changed status to InDialogue.");
        } else {
             Debug.Log($"[DialogueManager] Keeping status {statusBeforeDialogue} during dialogue.");
        }

        currentLines = lines;
        currentLineIndex = 0;
        dialoguePanel.SetActive(true);

        // Start the process
        DisplayCurrentLine(requiresInput); // Pass the requiresInput flag
    }


    // --- Public Methods for Non-Timeline Usage (Keep as is, but call internal logic) ---
    public void ShowDialogue(string text)
    {
        ShowDialogueInternal(new string[] { text }, false); // Assume single line doesn't require input unless specified
    }

    public void ShowDialogue(string[] lines)
    {
         if (lines == null || lines.Length == 0) return;
         ShowDialogueInternal(lines, true); // Assume multiple lines require input
    }


    // --- Timeline Integration Method ---
    /// <summary>
    /// Shows a single line of dialogue, specifically called from a Timeline clip.
    /// 显示单行对话，专门由 Timeline 片段调用。
    /// </summary>
    /// <param name="speaker">The speaker's name. / 说话者名字。</param>
    /// <param name="text">The dialogue text. / 对话文本。</param>
    /// <param name="requiresInput">If true, waits for player input to proceed (and complete). If false, might auto-advance based on Timeline clip duration. / 如果为 true，则等待玩家输入以继续（并完成）。如果为 false，则可能根据 Timeline 片段持续时间自动前进（但通常由 Timeline 控制）。</param>
    public void ShowTimelineDialogue(string speaker, string text, bool requiresInput)
    {
        Debug.Log($"[DialogueManager] ShowTimelineDialogue called. Speaker: {speaker}, RequiresInput: {requiresInput}");
        // Format the text to include the speaker
        // 格式化文本以包含说话者
        string formattedText = !string.IsNullOrEmpty(speaker) ? $"[{speaker}]: {text}" : text;

        // Use the internal logic, treating it as a single-line sequence
        // 使用内部逻辑，将其视为单行序列
        ShowDialogueInternal(new string[] { formattedText }, requiresInput);
    }
    // --- End Timeline Integration Method ---


    // --- Coroutines are no longer directly used for main flow control ---
    // They might be useful for text effects (like typewriter) later, but not for basic display.
    // --- 协程不再直接用于主流程控制 ---
    // 它们稍后可能对文本效果（如打字机）有用，但对于基本显示则不是。


    // --- Update Method (Modified Escape Logic) ---
    void Update()
    {
        // Only process input if dialogue is active AND waiting for player input
        // 仅当对话激活并等待玩家输入时处理输入
        if (IsDialogueActive && waitingForInput)
        {
            // Escape key to skip/hide the current dialogue segment
            // Escape 键跳过/隐藏当前对话段落
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[DialogueManager] Dialogue skipped by player (Escape pressed). Hiding dialogue.");
                HideDialogue(); // Call HideDialogue to ensure proper cleanup and event triggering
                return; // Stop checking other inputs for this frame
            }

            // Standard progression keys
            // 标准推进键
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                Debug.Log("[DialogueManager] Player proceeding to next line.");
                ProceedToNextLine();
            }
        }
    }

    // --- Core Display Logic ---
    /// <summary>
    /// Displays the current line based on currentLineIndex.
    /// 根据 currentLineIndex 显示当前行。
    /// </summary>
    /// <param name="waitForInputFlag">Explicitly sets if this line requires waiting for input. / 明确设置此行是否需要等待输入。</param>
    private void DisplayCurrentLine(bool waitForInputFlag)
    {
        if (currentLines == null || currentLineIndex >= currentLines.Length)
        {
            Debug.Log("[DialogueManager] No more lines or lines array is null. Hiding dialogue.");
            HideDialogue(); // No more lines, hide the dialogue
            return;
        }

        dialogueText.text = currentLines[currentLineIndex];
        waitingForInput = waitForInputFlag; // Set based on the flag passed in
        Debug.Log($"[DialogueManager] Displaying line {currentLineIndex + 1}/{currentLines.Length}. Waiting for input: {waitingForInput}");

        // If NOT waiting for input, Timeline duration should handle when the clip ends.
        // DialogueManager doesn't need its own timer here when called from Timeline.
        // 如果不等待输入，Timeline 片段的持续时间应处理何时结束。
        // 从 Timeline 调用时，DialogueManager 此处不需要自己的计时器。
    }

    /// <summary>
    /// Proceeds to the next line or hides dialogue if it's the last line.
    ///前进到下一行，如果是最后一行则隐藏对话。
    /// </summary>
    private void ProceedToNextLine()
    {
        currentLineIndex++;
        // Check if there are more lines AFTER incrementing
        if (currentLines != null && currentLineIndex < currentLines.Length)
        {
            // Still more lines, display the next one. Assume subsequent lines always wait for input.
             // 仍然有更多行，显示下一行。假设后续行总是等待输入。
            DisplayCurrentLine(true);
        }
        else
        {
            // This was the last line.
            // 这是最后一行。
            Debug.Log("[DialogueManager] Last line processed. Hiding dialogue.");
            HideDialogue();
        }
    }

    /// <summary>
    /// Stops any currently running dialogue coroutine.
    /// 停止任何当前正在运行的对话协程。
    /// </summary>
    private void StopExistingDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
            Debug.Log("[DialogueManager] Stopped existing dialogue coroutine.");
        }
         // Reset flags just in case, though HideDialogue should handle most cleanup
         // 以防万一重置标志，尽管 HideDialogue 应该处理大部分清理工作
        // waitingForInput = false; // HideDialogue handles this
    }


    // --- Hide Dialogue Method (Modified) ---
    /// <summary>
    /// Hides the dialogue panel, resets state, restores game status, and triggers completion events.
    /// 隐藏对话面板，重置状态，恢复游戏状态，并触​​发完成事件。
    /// </summary>
    public void HideDialogue()
    {
        // Ensure this only runs once per dialogue sequence activation
        // 确保每次对话序列激活只运行一次
        if (!IsDialogueActive)
        {
            Debug.LogError("现在对话处于未激活状态");
            // Debug.Log("[DialogueManager] HideDialogue called but IsDialogueActive is false. Ignoring.");
            return;
        }

        Debug.Log("[DialogueManager] Hiding dialogue panel and resetting state.");
        StopExistingDialogue(); // Stop coroutines just in case

        dialoguePanel.SetActive(false);
        IsDialogueActive = false;
        waitingForInput = false;
        // currentLines = null; // Keep nulling this? Maybe not necessary if always set at start.

        EventManager.Instance?.TriggerEvent(new DialogueStateChangedEvent(false)); // Notify listeners dialogue ended

        // --- Conditional State Restoration ---
        // Restore game status ONLY if dialogue started from 'Playing' state
        // 仅当对话从 'Playing' 状态开始时才恢复游戏状态
        if (statusBeforeDialogue == GameStatus.Playing)
        {
            // Check current status before changing, avoid overriding Pause, etc.
            // 在更改前检查当前状态，避免覆盖 Pause 等。
            if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.InDialogue)
            {
                GameRunManager.Instance.ChangeGameStatus(GameStatus.Playing);
                Debug.Log("[DialogueManager] Restored status to Playing.");
            } else {
                Debug.LogWarning($"[DialogueManager] Tried to restore to Playing, but current status is {GameRunManager.Instance?.CurrentStatus ?? GameStatus.InMenu}. Status not changed.");
            }
        }
        else // If dialogue started during Cutscene or other non-Playing state, leave the current status alone
        {
            Debug.Log($"[DialogueManager] Dialogue ended. Kept status: {GameRunManager.Instance?.CurrentStatus ?? statusBeforeDialogue}");
        }
        // --- End Conditional State Restoration ---


        // --- Trigger Timeline Completion Event ---
        // IMPORTANT: Trigger the specific Timeline callback if it has subscribers.
        // 重要：如果 Timeline 回调有订阅者，则触发它。
        if (onTimelineDialogueComplete != null)
        {
            Debug.Log("[DialogueManager] Triggering onTimelineDialogueComplete event.");
            onTimelineDialogueComplete?.Invoke();
            onTimelineDialogueComplete = null; // Clear the event listeners after invoking
        }
        // --- End Timeline Completion Event ---

         // Reset for next sequence
         // 为下一个序列重置
         currentLines = null;
         currentLineIndex = 0;
    }
}
