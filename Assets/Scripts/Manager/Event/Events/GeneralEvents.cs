// 任务完成事件
public class QuestCompletedEvent
{
    public string QuestId { get; private set; }
    public QuestCompletedEvent(string questId)
    {
        QuestId = questId;
    }
}

// 物品拾取事件
public class ItemPickedUpEvent
{
    public string ItemName { get; private set; }
    public ItemPickedUpEvent(string itemName)
    {
        ItemName = itemName;
    }
}

// 传送事件
public class TransitionRequestedEvent
{
    public string TargetSceneName { get; private set; }
    public string TargetTeleporterID { get; private set; }

    public TransitionRequestedEvent(string targetSceneName, string targetTeleporterID)
    {
        TargetSceneName = targetSceneName;
        TargetTeleporterID = targetTeleporterID;
    }
}

// 阶段改变事件
public class StageChangeEvent
{
    public int StageId {get; private set;}
    public StageChangeEvent(int stageId)
    {
        StageId = stageId;
    }
}

// Game status change event
public class GameStatusChangedEvent
{
    public GameStatus PreviousStatus { get; private set; }
    public GameStatus NewStatus { get; private set; }
    public GameStatusChangedEvent(GameStatus newStatus, GameStatus prevStatus)
    {
        NewStatus = newStatus;
        PreviousStatus = prevStatus;
    }
}