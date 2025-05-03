/// <summary>
/// 包含与游戏会话启动和结束相关的全局事件。
/// Contains global events related to game session start and end.
/// </summary>

// 游戏开始请求事件 (由主菜单等触发)
// Game start request event (triggered by main menu etc.)
public class GameStartEvent
{
    public GameStartEvent() { }
}

// 游戏结束请求事件 (由 GameOverUI 或暂停菜单触发)
// Game end request event (triggered by GameOverUI or pause menu)
public class GameEndEvent
{
    public GameEndEvent() { }
}


// --- 新增事件：游戏准备好开始玩 ---
// --- NEW Event: Game is Ready to Play ---
/// <summary>
/// 当所有初始场景加载完成、玩家设置完毕、游戏状态已切换到 Playing 后触发。
/// Triggered after all initial scenes are loaded, player is set up,
/// and the game status has been switched to Playing.
/// </summary>
public class GameReadyToPlayEvent
{
    // 可以根据需要添加数据，例如传递第一个关卡的场景名称等
    // Can add data if needed, e.g., pass the first level's scene name
    public GameReadyToPlayEvent() { }
}
