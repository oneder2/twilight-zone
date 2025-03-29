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

// 阶段改变事件
public class StageChangeEvent
{
    public int StageId {get; private set;}
    public StageChangeEvent(int stageId)
    {
        StageId = stageId;
    }
}