/// <summary>
/// 定义游戏过程中可能发生的全局事件。
/// Defines global events that might occur during gameplay.
/// </summary>

// 示例：当Timeline需要打开特定门时触发的事件
// Example: Event triggered when a Timeline needs a specific door to open.
public class OpenSpecificDoorEvent
{
    public string DoorID { get; private set; }
    public OpenSpecificDoorEvent(string doorId) { DoorID = doorId; }
}

// 示例：一个不需要传递数据的简单Timeline动作事件
// Example: A simple Timeline action event that doesn't need data.
public class MyTimelineActionEvent
{
    public MyTimelineActionEvent() { }
}

// --- Character Outcome Events ---
public class CrushsisOutcomeEvent { public CharacterOutcome Outcome { get; private set; } public CrushsisOutcomeEvent(CharacterOutcome outcome) { Outcome = outcome; } }
public class FriendOutcomeEvent { public CharacterOutcome Outcome { get; private set; } public FriendOutcomeEvent(CharacterOutcome outcome) { Outcome = outcome; } }
public class CrushOutcomeEvent { public CharacterOutcome Outcome { get; private set; } public CrushOutcomeEvent(CharacterOutcome outcome) { Outcome = outcome; } }
public class TeacherOutcomeEvent { public CharacterOutcome Outcome { get; private set; } public TeacherOutcomeEvent(CharacterOutcome outcome) { Outcome = outcome; } }

// --- Other Gameplay Events ---
public class FinalActEvent { public FinalActEvent() { } }
public class EndingCheckRequestedEvent { public EndingCheckRequestedEvent() { } }
public class TargetSavedEvent { public string TargetName { get; private set; } public CharacterOutcome Outcome { get; private set; } public TargetSavedEvent(string targetName, CharacterOutcome outcome) { TargetName = targetName; Outcome = outcome; } }
public class LoopCompletedEvent { public int CompletedLoopNumber { get; private set; } public LoopCompletedEvent(int completedLoopNumber) { CompletedLoopNumber = completedLoopNumber; } }


// --- NEW: Cutscene Sequence Completed Event ---
// --- 新增：过场动画序列完成事件 ---
/// <summary>
/// 当一个特定的、重要的 Timeline 序列播放完毕时触发。
/// Triggered when a specific, important Timeline sequence finishes playing.
/// </summary>
public class CutsceneSequenceCompletedEvent
{
    /// <summary>
    /// 刚刚完成的序列的唯一标识符 (例如 "BeginnerDeathSequence").
    /// The unique identifier of the sequence that just finished (e.g., "BeginnerDeathSequence").
    /// </summary>
    public string SequenceIdentifier { get; private set; }

    public CutsceneSequenceCompletedEvent(string identifier)
    {
        SequenceIdentifier = identifier;
    }
}
