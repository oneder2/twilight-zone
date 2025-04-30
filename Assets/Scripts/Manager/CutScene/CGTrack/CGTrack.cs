using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// --- Define your CG Manager type here ---
// --- 在此处定义你的 CG 管理器类型 ---
// Ensure CutsceneUIManager is accessible (add 'using YourNamespace;' if needed)
// 确保 CutsceneUIManager 可访问（如果需要，添加 'using YourNamespace;'）
using TargetManagerType = CutsceneUIManager; // Set to your actual CG Manager class / 设置为你的实际 CG 管理器类


/// <summary>
/// Defines a custom Timeline track for controlling full-screen CG display.
/// 定义一个用于控制全屏CG显示的自定义Timeline轨道。
/// Binds to a CutsceneUIManager component.
/// 绑定到一个 CutsceneUIManager 组件。
/// </summary>
[TrackColor(0.9f, 0.6f, 0.2f)] // Assign a distinct color / 指定一个独特的颜色
[TrackClipType(typeof(CGPlayableAsset))] // Accepts CGPlayableAsset clips / 接受 CGPlayableAsset 片段
[TrackBindingType(typeof(TargetManagerType))] // Binds to CutsceneUIManager / 绑定到 CutsceneUIManager
public class CGTrack : TrackAsset
{
    /// <summary>
    /// Creates the Track Mixer Playable for this CG track.
    /// 为此CG轨道创建轨道混合器 Playable。
    /// The mixer passes the bound CutsceneUIManager reference to the clip behaviours.
    /// 混合器将绑定的 CutsceneUIManager 引用传递给片段行为。
    /// </summary>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        // Create the mixer behaviour playable
        var mixer = ScriptPlayable<CGTrackMixerBehaviour>.Create(graph, inputCount);
        CGTrackMixerBehaviour mixerBehaviour = mixer.GetBehaviour();

        // Get the bound CG Manager instance
        PlayableDirector director = go.GetComponent<PlayableDirector>();
        if (director != null)
        {
            // Get the object bound to this track
            UnityEngine.Object genericBinding = director.GetGenericBinding(this);
            TargetManagerType boundManager = genericBinding as TargetManagerType; // Use the defined type

            // Check if the binding is correct
            if (boundManager != null)
            {
                // Pass the reference to the mixer behaviour
                mixerBehaviour.CGManager = boundManager;
            }
            else if (genericBinding != null)
            {
                 Debug.LogWarning($"CGTrack on '{go.name}' is bound to an object of type '{genericBinding.GetType().Name}', but expected '{typeof(TargetManagerType).Name}'. Please bind the correct CutsceneUIManager component.", go);
                 // 警告：'{go.name}' 上的 CGTrack 绑定到了类型为 '{genericBinding.GetType().Name}' 的对象，但期望的是 '{typeof(TargetManagerType).Name}'。请绑定正确的 CutsceneUIManager 组件。
            }
            else
            {
                Debug.LogWarning($"CGTrack on '{go.name}' requires a '{typeof(TargetManagerType).Name}' binding. Assign the CutsceneUIManager component to the track binding in the Timeline Editor.", go);
                // 警告：'{go.name}' 上的 CGTrack 需要一个 '{typeof(TargetManagerType).Name}' 绑定。请在 Timeline 编辑器中将 CutsceneUIManager 组件分配给轨道绑定。
            }
        }
        else
        {
             Debug.LogWarning($"PlayableDirector not found on '{go.name}'. CGTrack requires a PlayableDirector.", go);
             // 警告：在 '{go.name}' 上找不到 PlayableDirector。CGTrack 需要 PlayableDirector。
        }

        return mixer;
    }

     /// <summary>
    /// Gathers properties modified by this track (primarily the binding).
    /// 收集由此轨道修改的属性（主要是绑定）。
    /// </summary>
    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        // Register the binding type
        var binding = director.GetGenericBinding(this);
        if (binding is TargetManagerType)
        {
             // If you were animating properties *on* the CutsceneUIManager via the track itself, register them here.
        }
        base.GatherProperties(director, driver);
    }
}


/// <summary>
/// The mixer behaviour for the CGTrack. Passes the bound CutsceneUIManager
/// reference down to the active CGPlayableBehaviours.
/// CGTrack 的混合器行为。将绑定的 CutsceneUIManager
/// 引用传递给活动的 CGPlayableBehaviours。
/// </summary>
public class CGTrackMixerBehaviour : PlayableBehaviour
{
    // Reference to the bound CG Manager
    public TargetManagerType CGManager { get; set; } // Use the defined type

    /// <summary>
    /// Passes the CGManager reference to active clip behaviours each frame.
    /// 每帧将 CGManager 引用传递给活动的片段行为。
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (CGManager == null) return;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            if (inputWeight > 0f)
            {
                try
                {
                    ScriptPlayable<CGPlayableBehaviour> inputPlayable = (ScriptPlayable<CGPlayableBehaviour>)playable.GetInput(i);
                    CGPlayableBehaviour inputBehaviour = inputPlayable.GetBehaviour();

                    // Pass the manager reference
                    inputBehaviour.CGManager = this.CGManager; // Pass the concrete type
                }
                catch (InvalidCastException e)
                {
                     Debug.LogError($"Clip at index {i} on CGTrack is not a CGPlayableAsset. Found type: {playable.GetInput(i).GetPlayableType()}. Error: {e.Message}");
                }
            }
        }
    }
}


// --- Dummy Manager Class (Remove if CutsceneUIManager is defined elsewhere) ---
// --- 虚拟管理器类（如果 CutsceneUIManager 在别处定义，则移除） ---
/*
public class CutsceneUIManager : MonoBehaviour // Replace with your actual base class if needed
{
    public static CutsceneUIManager Instance; // Assuming Singleton
    void Awake() { Instance = this; } // Basic Singleton setup

    public void ShowFullscreenCG(string identifier, float fadeDuration) { Debug.Log($"Showing CG '{identifier}' with fade in {fadeDuration}s"); }
    public void HideAllFullscreenCGs(float fadeDuration) { Debug.Log($"Hiding all CGs with fade out {fadeDuration}s"); }
}
*/
