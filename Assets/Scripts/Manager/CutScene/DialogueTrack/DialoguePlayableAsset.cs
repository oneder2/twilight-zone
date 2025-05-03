using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System; // Required for Serializable attribute

/// <summary>
/// Represents the data for a dialogue clip on the Timeline.
/// It stores the dialogue text, speaker, and behaviour flags.
/// 代表Timeline上一个对话片段的数据。
/// 它存储对话文本、说话者和行为标志。
/// </summary>
[Serializable] // Ensures this class can be serialized by Unity. / 确保这个类可以被Unity序列化。
public class DialoguePlayableAsset : PlayableAsset, ITimelineClipAsset
{
    // --- Exposed Properties in Inspector ---
    // --- 在Inspector中暴露的属性 ---

    [Tooltip("The name of the character speaking.")] // 提示信息
    [SerializeField] // 强制序列化私有字段 / Force serialization of private fields
    private string speakerName = "Narrator"; // 说话者名字 / Speaker's name

    [Tooltip("The dialogue text to display.")] // 提示信息
    [TextArea(3, 10)] // Makes the string field a text area in the Inspector. / 使字符串字段在Inspector中显示为文本区域。
    [SerializeField]
    private string dialogueText = "Enter dialogue here..."; // 对话文本 / Dialogue text

    [Tooltip("If true, the Timeline will pause until the dialogue is completed (e.g., player clicks continue).")] // 提示信息
    [SerializeField]
    private bool pauseTimelineUntilFinished = true; // 是否暂停Timeline直到对话完成 / Whether to pause Timeline until dialogue is finished

    // --- ITimelineClipAsset Implementation ---
    // --- ITimelineClipAsset 接口实现 ---

    /// <summary>
    /// Defines the capabilities of this clip type (e.g., blending, extrapolation).
    /// None is typical for dialogue clips as blending text doesn't usually make sense.
    /// 定义此片段类型的功能（例如，混合、外插）。
    /// 对于对话片段，通常使用None，因为混合文本通常没有意义。
    /// </summary>
    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    // --- PlayableAsset Implementation ---
    // --- PlayableAsset 接口实现 ---

    /// <summary>
    /// Creates the runtime instance (PlayableBehaviour) for this clip.
    /// Called by the Timeline system when the clip needs to be played.
    /// 为此片段创建运行时实例（PlayableBehaviour）。
    /// 当需要播放此片段时由Timeline系统调用。
    /// </summary>
    /// <param name="graph">The PlayableGraph that owns the playable. / 拥有此Playable的PlayableGraph。</param>
    /// <param name="owner">The GameObject that owns the PlayableDirector component. / 拥有PlayableDirector组件的GameObject。</param>
    /// <returns>A configured ScriptPlayable holding the DialoguePlayableBehaviour. / 一个配置好的、包含DialoguePlayableBehaviour的ScriptPlayable。</returns>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        // 1. Create an instance of the ScriptPlayable<T> templated on our behaviour type.
        // 1. 创建一个基于我们行为类型的 ScriptPlayable<T> 实例。
        var playable = ScriptPlayable<DialoguePlayableBehaviour>.Create(graph);

        // 2. Get the behaviour instance from the playable.
        // 2. 从Playable中获取行为实例。
        DialoguePlayableBehaviour dialogueBehaviour = playable.GetBehaviour();

        // 3. Pass the data from this asset (Inspector values) to the behaviour instance.
        // 3. 将此资产（Inspector中的值）的数据传递给行为实例。
        dialogueBehaviour.SpeakerName = this.speakerName;
        dialogueBehaviour.DialogueText = this.dialogueText;
        dialogueBehaviour.PauseTimelineUntilFinished = this.pauseTimelineUntilFinished;
        // Note: The DialogueManager reference is passed later via the TrackMixer.
        // 注意：DialogueManager 的引用稍后通过 TrackMixer 传递。

        return playable;
    }
}
