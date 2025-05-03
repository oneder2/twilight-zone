/// <summary>
/// 包含与过场动画 UI (如 CG 背景) 相关的全局事件定义。
/// Contains global event definitions related to cutscene UI elements (like CG backgrounds).
/// </summary>

// --- 新增事件：请求显示/切换全屏 CG ---
// --- NEW Event: Request to Show/Switch Fullscreen CG ---
public class ShowCGRequestedEvent
{
    /// <summary>
    /// 要显示的 CG 的唯一标识符 (来自 CutsceneUIManager 的 CG Library)。
    /// The unique identifier of the CG to display (from CutsceneUIManager's CG Library).
    /// </summary>
    public string CgIdentifier { get; private set; }

    /// <summary>
    /// 交叉淡入淡出的时长（秒）。负数表示使用默认值。
    /// The duration of the crossfade in seconds. Negative value means use default.
    /// </summary>
    public float FadeDuration { get; private set; }

    /// <summary>
    /// 构造函数 (Constructor)
    /// </summary>
    /// <param name="identifier">CG 标识符 (CG Identifier)</param>
    /// <param name="duration">淡入淡出时长 (Fade Duration)</param>
    public ShowCGRequestedEvent(string identifier, float duration = -1f)
    {
        CgIdentifier = identifier;
        FadeDuration = duration;
    }
}

// --- 新增事件：请求隐藏所有全屏 CG ---
// --- NEW Event: Request to Hide All Fullscreen CGs ---
public class HideAllCGsRequestedEvent
{
    /// <summary>
    /// 淡出的时长（秒）。负数表示使用默认值。
    /// The duration of the fade out in seconds. Negative value means use default.
    /// </summary>
    public float FadeDuration { get; private set; }

    /// <summary>
    /// 构造函数 (Constructor)
    /// </summary>
    /// <param name="duration">淡出时长 (Fade Duration)</param>
    public HideAllCGsRequestedEvent(float duration = -1f)
    {
        FadeDuration = duration;
    }
}