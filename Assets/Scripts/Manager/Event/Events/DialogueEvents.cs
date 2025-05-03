using UnityEngine.Playables; // 需要引用 Playables 命名空间

/// <summary>
/// 包含与对话系统相关的全局事件定义。
/// Contains global event definitions related to the dialogue system.
/// </summary>

public class DialogueStateChangedEvent
{
    public bool IsInDialogue { get; private set; }
    public DialogueStateChangedEvent(bool isInDialogue) { IsInDialogue = isInDialogue; }
}

// 用于请求显示普通对话（可能由事件触发，但不一定暂停Timeline）
// For requesting normal dialogue display (might be event-triggered, doesn't necessarily pause Timeline)
public class ShowBlockingDialogueRequestedEvent
{
    public string[] Lines { get; private set; }
    public ShowBlockingDialogueRequestedEvent(string[] linesToShow) { Lines = linesToShow ?? new string[0]; }
    public ShowBlockingDialogueRequestedEvent(string singleLineToShow) { Lines = new string[] { singleLineToShow ?? string.Empty }; }
}


// --- 新增事件：请求显示可暂停Timeline的对话 ---
// --- NEW Event: Request dialogue display that pauses the Timeline ---
/// <summary>
/// 当 Timeline 请求显示需要玩家交互（并因此需要暂停 Timeline）的对话时触发。
/// Triggered when the Timeline requests dialogue that requires player interaction
/// (and thus needs the Timeline to pause).
/// </summary>
public class ShowPausableDialogueRequestedEvent
{
    /// <summary>
    /// 要显示的对话文本行。
    /// The lines of dialogue text to display.
    /// </summary>
    public string[] Lines { get; private set; }

    /// <summary>
    /// 触发此请求的 PlayableDirector 实例，以便 GameRunManager 可以暂停它。
    /// The PlayableDirector instance that triggered this request, so GameRunManager can pause it.
    /// </summary>
    public PlayableDirector DirectorToPause { get; private set; }

    /// <summary>
    /// 构造函数 (Constructor)
    /// </summary>
    /// <param name="linesToShow">要显示的对话文本数组 (Array of dialogue lines to show)</param>
    /// <param name="director">需要暂停的 PlayableDirector (The PlayableDirector to pause)</param>
    public ShowPausableDialogueRequestedEvent(string[] linesToShow, PlayableDirector director)
    {
        Lines = linesToShow ?? new string[0];
        DirectorToPause = director;
    }

     /// <summary>
    /// 构造函数重载，方便传递单行文本。
    /// Constructor overload for easily passing a single line of text.
    /// </summary>
    /// <param name="singleLineToShow">要显示的单行对话文本 (Single line of dialogue to show)</param>
    /// <param name="director">需要暂停的 PlayableDirector (The PlayableDirector to pause)</param>
    public ShowPausableDialogueRequestedEvent(string singleLineToShow, PlayableDirector director)
    {
        Lines = new string[] { singleLineToShow ?? string.Empty };
        DirectorToPause = director;
    }
}


// --- 新增事件：通知可暂停对话序列已完成 ---
// --- NEW Event: Notifies that a pausable dialogue sequence is complete ---
/// <summary>
/// 当一个由 ShowPausableDialogueRequestedEvent 启动的对话序列完成（玩家看完了所有行）时触发。
/// Triggered when a dialogue sequence initiated by ShowPausableDialogueRequestedEvent
/// is completed (player has viewed all lines).
/// </summary>
public class DialogueSequenceCompleteEvent
{
    // 可以选择性地添加完成时需要传递的数据
    // Optionally add data to pass upon completion
    public DialogueSequenceCompleteEvent() { }
}
