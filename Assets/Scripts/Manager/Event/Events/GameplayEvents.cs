/// <summary>
/// 定义游戏过程中可能发生的全局事件。
/// Defines global events that might occur during gameplay.
/// </summary>

// 示例：当Timeline需要打开特定门时触发的事件
// Example: Event triggered when a Timeline needs a specific door to open.
public class OpenSpecificDoorEvent
{
    /// <summary>
    /// 需要被打开的门的唯一标识符。
    /// The unique identifier of the door that needs to be opened.
    /// </summary>
    public string DoorID { get; private set; }

    /// <summary>
    /// 构造函数 (Constructor)
    /// </summary>
    /// <param name="doorId">要打开的门的ID (The ID of the door to open)</param>
    public OpenSpecificDoorEvent(string doorId)
    {
        DoorID = doorId;
    }
}

// 示例：一个不需要传递数据的简单Timeline动作事件
// Example: A simple Timeline action event that doesn't need data.
public class MyTimelineActionEvent
{
    // 这个事件类是空的，因为它只表示一个信号的发生。
    // This event class is empty as it just signifies an action occurred.
    public MyTimelineActionEvent() { }
}

// 你可以在这个文件里根据需要添加更多自定义的游戏事件类。
// You can add more custom gameplay event classes to this file as needed.
