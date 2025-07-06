using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Defines a custom Timeline track specifically for dialogue clips.
/// It specifies the type of clips it accepts (DialoguePlayableAsset)
/// and the type of object it needs to be bound to (DialogueManager).
/// 定义一个专门用于对话片段的自定义Timeline轨道。
/// 它指定了它接受的片段类型（DialoguePlayableAsset）
/// 以及它需要绑定到的对象类型（DialogueManager）。
/// </summary>
[TrackColor(0.8f, 0.3f, 0.9f)] // Sets the color of the track in the Timeline editor / 设置轨道在Timeline编辑器中的颜色
[TrackClipType(typeof(DialoguePlayableAsset))] // Specifies that this track holds DialoguePlayableAsset clips / 指定此轨道包含 DialoguePlayableAsset 片段
[TrackBindingType(typeof(DialogueManager))] // Specifies that this track should be bound to a DialogueManager component / 指定此轨道应绑定到 DialogueManager 组件
public class DialogueTrack : TrackAsset
{
    /// <summary>
    /// Creates the Track Mixer Playable for this track.
    /// The mixer is responsible for processing the input from all clips on the track
    /// and passing the bound DialogueManager reference to the clip behaviours.
    /// 为此轨道创建轨道混合器 Playable。
    /// 混合器负责处理轨道上所有片段的输入，
    /// 并将绑定的 DialogueManager 引用传递给片段行为。
    /// </summary>
    /// <param name="graph">The PlayableGraph. / PlayableGraph。</param>
    /// <param name="go">The GameObject owning the PlayableDirector. / 拥有 PlayableDirector 的 GameObject。</param>
    /// <param name="inputCount">The number of clips connected to this track. / 连接到此轨道的片段数量。</param>
    /// <returns>A Playable representing the track mixer. / 代表轨道混合器的 Playable。</returns>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        // Create the mixer behaviour playable
        // 创建混合器行为 Playable
        var mixer = ScriptPlayable<DialogueTrackMixerBehaviour>.Create(graph, inputCount);

        // Get the behaviour instance from the mixer playable
        // 从混合器 Playable 获取行为实例
        DialogueTrackMixerBehaviour mixerBehaviour = mixer.GetBehaviour();

        // Find the PlayableDirector component on the owner GameObject
        // 在所有者 GameObject 上查找 PlayableDirector 组件
        PlayableDirector director = go.GetComponent<PlayableDirector>();
        if (director != null)
        {
            // Get the object bound to this specific track instance in the Timeline editor.
            // 获取在 Timeline 编辑器中绑定到此特定轨道实例的对象。
            UnityEngine.Object genericBinding = director.GetGenericBinding(this); // Get the raw binding first / 首先获取原始绑定
            DialogueManager boundManager = genericBinding as DialogueManager; // Attempt to cast / 尝试转换

            if (boundManager != null)
            {
                // Pass the bound DialogueManager reference to the mixer behaviour.
                // 将绑定的 DialogueManager 引用传递给混合器行为。
                mixerBehaviour.DialogueManager = boundManager;
            }
            else if (genericBinding != null) // Check if something was bound, but wrong type / 检查是否绑定了对象，但类型错误
            {
                 Debug.LogWarning($"DialogueTrack on '{go.name}' is bound to an object of type '{genericBinding.GetType().Name}', but expected 'DialogueManager'. Please bind a GameObject with a DialogueManager component.", go);
                 // 警告：'{go.name}' 上的 DialogueTrack 绑定到了类型为 '{genericBinding.GetType().Name}' 的对象，但期望的是 'DialogueManager'。请绑定一个带有 DialogueManager 组件的 GameObject。
            }
            else // Nothing was bound / 没有绑定任何对象
            {
                Debug.LogWarning($"DialogueTrack on '{go.name}' requires a DialogueManager binding. Assign a GameObject with a DialogueManager component to the track binding in the Timeline Editor.", go);
                // 警告：'{go.name}' 上的 DialogueTrack 需要一个 DialogueManager 绑定。请在 Timeline 编辑器中将带有 DialogueManager 组件的 GameObject 分配给轨道绑定。
            }
        }
        else
        {
             Debug.LogWarning($"PlayableDirector not found on '{go.name}'. DialogueTrack requires a PlayableDirector.", go);
             // 警告：在 '{go.name}' 上找不到 PlayableDirector。DialogueTrack 需要 PlayableDirector。
        }


        // The mixer behaviour itself doesn't *do* much for dialogue besides passing the manager,
        // but it's the standard way to handle track-level data and bindings.
        // 对于对话来说，混合器行为本身除了传递管理器之外*做*不了太多事情，
        // 但这是处理轨道级数据和绑定的标准方法。
        return mixer;
    }

     /// <summary>
    /// Called when Timeline rebuilds the graph (e.g., adding/removing clips).
    /// Use this to potentially clear cached data if needed, though often not required for simple tracks.
    /// 当 Timeline 重建图时调用（例如，添加/删除片段）。
    /// 如果需要，可以使用它来清除缓存数据，尽管对于简单轨道通常不需要。
    /// </summary>
    /// <param name="director">The PlayableDirector whose graph is being rebuilt. / 正在重建其图的 PlayableDirector。</param>
    /// <param name="driver">An interface for collecting property modifications driven by the Timeline. / 用于收集由 Timeline 驱动的属性修改的接口。</param>
    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        // This method is used for animating properties directly via Timeline curves.
        // For our dialogue track, we bind to a component (DialogueManager), so we
        // inform the system about this binding. This helps Unity optimize and manage dependencies.
        // 此方法用于通过 Timeline 曲线直接为属性设置动画。
        // 对于我们的对话轨道，我们绑定到一个组件 (DialogueManager)，所以我们
        // 将此绑定告知系统。这有助于 Unity 优化和管理依赖关系。

        // Get the bound object for this track
        // 获取此轨道的绑定对象
        UnityEngine.Object binding = director.GetGenericBinding(this);
        if (binding is DialogueManager)
        {
             // If you were animating properties *on* the DialogueManager via the track itself
             // (not typical for this setup), you would register them here using driver.AddFromName<>().
             // Example: driver.AddFromName<DialogueManager>(binding.gameObject, "somePropertyToAnimate");
             // 如果你是通过轨道本身为 DialogueManager *上*的属性设置动画
             // （对于此设置来说不典型），你可以在此处使用 driver.AddFromName<>() 注册它们。
             // 示例：driver.AddFromName<DialogueManager>(binding.gameObject, "somePropertyToAnimate");
        }
        // Important: Call the base implementation AFTER processing your specific bindings.
        // 重要：在处理完特定绑定之后调用基类实现。
        base.GatherProperties(director, driver);
    }
}


/// <summary>
/// The mixer behaviour for the DialogueTrack. Its primary role here is to
/// receive the bound DialogueManager from the track and pass it down to
/// the active DialoguePlayableBehaviours each frame.
/// DialogueTrack 的混合器行为。它在这里的主要作用是
/// 从轨道接收绑定的 DialogueManager，并在每一帧将其传递给
/// 活动的 DialoguePlayableBehaviours。
/// </summary>
public class DialogueTrackMixerBehaviour : PlayableBehaviour
{
    // --- Reference to the bound DialogueManager ---
    // --- 对绑定的 DialogueManager 的引用 ---
    public DialogueManager DialogueManager { get; set; }

    /// <summary>
    /// Called every frame the Timeline is playing.
    /// Iterates through all connected clips (inputs) and passes the DialogueManager
    /// reference to the behaviour of any active clip.
    /// Timeline 播放的每一帧都会调用。
    /// 遍历所有连接的片段（输入）并将 DialogueManager
    /// 引用传递给任何活动片段的行为。
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // If no DialogueManager is bound, there's nothing to do.
        // 如果没有绑定 DialogueManager，则无事可做。
        if (DialogueManager == null)
        {
            return;
        }

        int inputCount = playable.GetInputCount(); // Get the number of clips connected to this track / 获取连接到此轨道的片段数量

        for (int i = 0; i < inputCount; i++)
        {
            // Get the weight of the current clip (0 if inactive, >0 if active/blending)
            // 获取当前片段的权重（如果不活动则为0，如果活动/混合则>0）
            float inputWeight = playable.GetInputWeight(i);

            // If the clip has any influence in this frame...
            // 如果片段在这一帧中有任何影响...
            if (inputWeight > 0f)
            {
                // Get the playable corresponding to the clip
                // 获取与片段对应的 Playable
                // Use a try-catch or check the type robustly if other playable types could exist on the track by mistake
                // 如果轨道上可能错误地存在其他 playable 类型，请使用 try-catch 或稳健地检查类型
                try
                {
                    ScriptPlayable<DialoguePlayableBehaviour> inputPlayable = (ScriptPlayable<DialoguePlayableBehaviour>)playable.GetInput(i);

                    // Get the behaviour instance from that playable
                    // 从该 Playable 获取行为实例
                    DialoguePlayableBehaviour inputBehaviour = inputPlayable.GetBehaviour();

                    // --- Pass the DialogueManager reference ---
                    // --- 传递 DialogueManager 引用 ---
                    // This ensures the behaviour has the manager reference *before* its PrepareFrame/ProcessFrame runs.
                    // 这确保了行为在运行其 PrepareFrame/ProcessFrame *之前*拥有管理器引用。
                    inputBehaviour.DialogueManager = this.DialogueManager;
                }
                catch (InvalidCastException e)
                {
                     // Log an error if a clip on this track is not of the expected type
                     // 如果此轨道上的片段不是预期类型，则记录错误
                     Debug.LogError($"Clip at index {i} on DialogueTrack is not a DialoguePlayableAsset. Found type: {playable.GetInput(i).GetPlayableType()}. Error: {e.Message}");
                }
            }
        }
    }
}
