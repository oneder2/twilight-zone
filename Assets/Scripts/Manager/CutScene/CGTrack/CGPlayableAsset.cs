using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System;

/// <summary>
/// Represents the data for a full-screen CG clip on the Timeline.
/// 代表Timeline上一个全屏CG片段的数据。
/// Stores the CG identifier and fade durations.
/// 存储CG标识符和淡入淡出时长。
/// </summary>
[Serializable]
public class CGPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    // --- Exposed Properties in Inspector ---
    // --- 在Inspector中暴露的属性 ---

    [Tooltip("Identifier for the CG resource (e.g., image name, resource path, or ID).")]
    [SerializeField]
    private string cgIdentifier = "Default_CG"; // CG 标识符 / CG Identifier

    [Tooltip("Duration in seconds for the CG to fade in. Negative value uses default.")]
    [SerializeField]
    private float fadeInDuration = 0.5f; // 淡入时长 / Fade-in duration

    [Tooltip("Duration in seconds for the CG to fade out when the clip ends. Negative value uses default.")]
    [SerializeField]
    private float fadeOutDuration = 0.5f; // 淡出时长 / Fade-out duration

    // --- ITimelineClipAsset Implementation ---
    // --- ITimelineClipAsset 接口实现 ---

    /// <summary>
    /// Defines the capabilities of this clip type. Typically None for simple display clips.
    /// 定义此片段类型的功能。对于简单的显示片段，通常为 None。
    /// </summary>
    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; } // No blending, looping, etc. by default / 默认不支持混合、循环等
    }

    // --- PlayableAsset Implementation ---
    // --- PlayableAsset 接口实现 ---

    /// <summary>
    /// Creates the runtime instance (PlayableBehaviour) for this CG clip.
    /// 为此CG片段创建运行时实例（PlayableBehaviour）。
    /// </summary>
    /// <param name="graph">The PlayableGraph that owns the playable. / 拥有此Playable的PlayableGraph。</param>
    /// <param name="owner">The GameObject that owns the PlayableDirector component. / 拥有PlayableDirector组件的GameObject。</param>
    /// <returns>A configured ScriptPlayable holding the CGPlayableBehaviour. / 一个配置好的、包含CGPlayableBehaviour的ScriptPlayable。</returns>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        // Create the behaviour playable
        // 创建行为 Playable
        var playable = ScriptPlayable<CGPlayableBehaviour>.Create(graph);

        // Get the behaviour instance
        // 获取行为实例
        CGPlayableBehaviour cgBehaviour = playable.GetBehaviour();

        // Pass data from this asset to the behaviour
        // 将此资产的数据传递给行为
        cgBehaviour.CgIdentifier = this.cgIdentifier;
        cgBehaviour.FadeInDuration = this.fadeInDuration;
        cgBehaviour.FadeOutDuration = this.fadeOutDuration;
        // The CG Manager reference is passed later via the TrackMixer.
        // CG 管理器的引用稍后通过 TrackMixer 传递。

        return playable;
    }
}
