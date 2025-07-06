using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Runtime component for a CG clip. Primarily holds data (identifier, fade durations)
/// for the CGTrackMixerBehaviour to use. The mixer now handles the core logic.
/// CG片段的运行时组件。主要持有数据（标识符、淡入淡出时长）
/// 供 CGTrackMixerBehaviour 使用。混合器现在处理核心逻辑。
/// </summary>
public class CGPlayableBehaviour : PlayableBehaviour
{
    // --- Data from CGPlayableAsset ---
    public string CgIdentifier { get; set; }
    public float FadeInDuration { get; set; }
    public float FadeOutDuration { get; set; }

    // --- Runtime Reference (Optional, set by Mixer) ---
    // --- 运行时引用（可选，由 Mixer 设置） ---
    // Although the behaviour doesn't directly use the manager much anymore,
    // the mixer still passes it, which might be useful for potential future extensions.
    // 虽然行为不再直接大量使用管理器，
    // 但混合器仍然传递它，这可能对未来潜在的扩展有用。
    public CutsceneUIManager CGManager { get; set; }

    // --- PlayableBehaviour Lifecycle Methods ---
    // Most logic previously here (calling Show/Hide) is now handled by the mixer.
    // We can keep these methods minimal or remove them if they do nothing.
    // 之前此处的大部分逻辑（调用 Show/Hide）现在由混合器处理。
    // 我们可以保持这些方法最小化，如果它们什么都不做，也可以移除它们。

    public override void OnGraphStart(Playable playable)
    {
        // Reset any internal state if needed when the graph starts
        // 图开始时根据需要重置任何内部状态
    }

    public override void OnGraphStop(Playable playable)
    {
        // Reset any internal state if needed when the graph stops
        // 图停止时根据需要重置任何内部状态
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        // Called when the clip starts playing within the Timeline.
        // 当片段在 Timeline 内开始播放时调用。
        // No direct action needed here anymore regarding CG display.
        // 此处不再需要关于 CG 显示的直接操作。
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Called when the clip stops playing (ends, or director paused/stopped).
        // 当片段停止播放时（结束，或导演暂停/停止）调用。
        // No direct action needed here anymore regarding CG display.
        // The mixer handles the fade-out when the *last* clip finishes.
        // 此处不再需要关于 CG 显示的直接操作。
        // 当 *最后一个* 片段完成时，混合器会处理淡出。
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        // Called every frame before ProcessFrame.
        // 在 ProcessFrame 之前每帧调用。
        // No direct action needed here anymore regarding CG display.
        // 此处不再需要关于 CG 显示的直接操作。
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // Called every frame while the clip is active.
        // 在片段活动期间每帧调用。
        // No direct action needed here anymore regarding CG display.
        // 此处不再需要关于 CG 显示的直接操作。
    }
}
