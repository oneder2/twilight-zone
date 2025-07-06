using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// --- Define your CG Manager type here ---
using TargetManagerType = CutsceneUIManager; // Ensure this matches your manager class

/// <summary>
/// Defines a custom Timeline track for controlling full-screen CG display.
/// Binds to a CutsceneUIManager component.
/// </summary>
[TrackColor(0.9f, 0.6f, 0.2f)]
[TrackClipType(typeof(CGPlayableAsset))]
[TrackBindingType(typeof(TargetManagerType))]
public class CGTrack : TrackAsset
{
    /// <summary>
    /// Creates the Track Mixer Playable for this CG track.
    /// The mixer now handles the logic for crossfading between CG clips.
    /// </summary>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        var mixer = ScriptPlayable<CGTrackMixerBehaviour>.Create(graph, inputCount);
        CGTrackMixerBehaviour mixerBehaviour = mixer.GetBehaviour();

        PlayableDirector director = go.GetComponent<PlayableDirector>();
        if (director != null)
        {
            UnityEngine.Object genericBinding = director.GetGenericBinding(this);
            TargetManagerType boundManager = genericBinding as TargetManagerType;

            if (boundManager != null)
            {
                mixerBehaviour.CGManager = boundManager;
                // Pass the director reference to the mixer if needed for time calculations, though often not necessary
                // mixerBehaviour.Director = director;
            }
            else if (genericBinding != null) {
                 Debug.LogWarning($"CGTrack on '{go.name}' is bound to an object of type '{genericBinding.GetType().Name}', but expected '{typeof(TargetManagerType).Name}'. Please bind the correct CutsceneUIManager component.", go);
            } else {
                Debug.LogWarning($"CGTrack on '{go.name}' requires a '{typeof(TargetManagerType).Name}' binding. Assign the CutsceneUIManager component to the track binding in the Timeline Editor.", go);
            }
        } else {
             Debug.LogWarning($"PlayableDirector not found on '{go.name}'. CGTrack requires a PlayableDirector.", go);
        }

        // Initialize mixer state
        mixerBehaviour.Initialize();

        return mixer;
    }

    // GatherProperties remains the same
    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
        var binding = director.GetGenericBinding(this);
        if (binding is TargetManagerType) { }
        base.GatherProperties(director, driver);
    }
}


/// <summary>
/// The mixer behaviour for the CGTrack. Handles detecting active clips
/// and instructing the CGManager to perform crossfades or fade out.
/// CGTrack 的混合器行为。处理检测活动片段
/// 并指示 CGManager 执行交叉淡入淡出或淡出。
/// </summary>
public class CGTrackMixerBehaviour : PlayableBehaviour
{
    // Reference to the bound CG Manager
    public TargetManagerType CGManager { get; set; }
    // Optional: Reference to the director if needed for time/state checks
    // public PlayableDirector Director { get; set; }

    // State tracking for the mixer
    private string currentActiveCgIdentifier = null;
    private float lastKnownFadeOutDuration = -1f; // Default fade duration from manager might be better
    private bool isInitialized = false; // Ensure Initialize runs once

    // Called once by CGTrack.CreateTrackMixer
    // 由 CGTrack.CreateTrackMixer 调用一次
    public void Initialize()
    {
        currentActiveCgIdentifier = null;
        lastKnownFadeOutDuration = -1f; // Or get default from CGManager if available
        isInitialized = true;
    }

     // Reset state when the graph starts playing
     // 图开始播放时重置状态
    public override void OnGraphStart(Playable playable)
    {
        // Re-initialize state in case the graph is restarted
        // 重新初始化状态以防图重新启动
        Initialize();
        // Ensure CGs are hidden at the start if the first clip doesn't start at time 0
        // 如果第一个片段不从时间 0 开始，确保 CG 在开始时是隐藏的
        // This might need refinement based on whether the Timeline starts playing from the beginning
        // 这可能需要根据 Timeline 是否从头开始播放进行细化
        // if (CGManager != null) CGManager.HideAllFullscreenCGs(0f); // Hide instantly at start?
    }

     // Cleanup when the graph stops
     // 图停止时进行清理
     public override void OnGraphStop(Playable playable)
    {
         // When the director stops entirely, ensure CGs are hidden
         // 当 director 完全停止时，确保 CG 被隐藏
         if (CGManager != null && currentActiveCgIdentifier != null) // If something was visible
         {
             Debug.Log("[CGTrackMixerBehaviour] Graph stopped. Hiding any active CG instantly.");
             CGManager.HideAllFullscreenCGs(0f); // Hide instantly
         }
         // Reset state
         Initialize();
    }


    /// <summary>
    /// Processes all clips on the track each frame to determine the active CG
    /// and trigger necessary fades via the CGManager.
    /// 每帧处理轨道上的所有片段以确定活动的 CG
    /// 并通过 CGManager 触发必要的淡入淡出。
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (CGManager == null || !isInitialized) return;

        string newlyActiveCgIdentifier = null;
        float fadeInDuration = -1f; // Use manager's default if not specified
        float fadeOutDuration = lastKnownFadeOutDuration; // Keep last known fade-out

        int inputCount = playable.GetInputCount();
        float highestWeight = 0f; // Track the highest weight found this frame

        // Find the clip that should be active (highest weight, or latest start time if weights equal)
        // 查找应该活动的片段（最高权重，如果权重相等则为最晚开始时间）
        // Simplified: Assume only one clip has weight > 0 due to ClipCaps.None
        // 简化：假设由于 ClipCaps.None，只有一个片段的权重 > 0
        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            highestWeight = Mathf.Max(highestWeight, inputWeight); // Track if *any* clip is active

            if (inputWeight > 0.99f) // Use a threshold slightly less than 1 for safety
            {
                try
                {
                    ScriptPlayable<CGPlayableBehaviour> inputPlayable = (ScriptPlayable<CGPlayableBehaviour>)playable.GetInput(i);
                    CGPlayableBehaviour inputBehaviour = inputPlayable.GetBehaviour();

                    // This is the currently dominant clip
                    // 这是当前占主导地位的片段
                    newlyActiveCgIdentifier = inputBehaviour.CgIdentifier;
                    fadeInDuration = inputBehaviour.FadeInDuration;
                    fadeOutDuration = inputBehaviour.FadeOutDuration; // Get fade out duration for *this* clip

                    // Pass manager reference (though it's less critical now)
                    // 传递管理器引用（尽管现在不那么重要了）
                    inputBehaviour.CGManager = this.CGManager;

                    break; // Assume only one clip is fully active
                }
                catch (InvalidCastException e) {
                     Debug.LogError($"Clip at index {i} on CGTrack is not a CGPlayableAsset. Error: {e.Message}");
                }
            }
        }

        // --- Logic for triggering fades ---

        // Case 1: A new CG clip has become active
        // 情况 1：一个新的 CG 片段变为活动状态
        if (newlyActiveCgIdentifier != null && newlyActiveCgIdentifier != currentActiveCgIdentifier)
        {
            Debug.Log($"[CGTrackMixerBehaviour] New active CG detected: '{newlyActiveCgIdentifier}'. Previous: '{currentActiveCgIdentifier ?? "None"}'. Triggering ShowFullscreenCG.");
            // Tell the manager to show the new CG (it handles the crossfade)
            // 告知管理器显示新的 CG（它会处理交叉淡入淡出）
            CGManager.ShowFullscreenCG(newlyActiveCgIdentifier, fadeInDuration);
            currentActiveCgIdentifier = newlyActiveCgIdentifier;
            lastKnownFadeOutDuration = fadeOutDuration; // Store the fade-out for when this one ends
        }
        // Case 2: No clip is active anymore, but one *was* active previously
        // 情况 2：不再有片段活动，但之前 *有* 一个活动
        else if (newlyActiveCgIdentifier == null && currentActiveCgIdentifier != null)
        {
             // This means the last active clip just finished
             // 这意味着最后一个活动片段刚刚结束
             Debug.Log($"[CGTrackMixerBehaviour] No active CG detected. Previously active: '{currentActiveCgIdentifier}'. Triggering HideAllFullscreenCGs.");
             // Tell the manager to hide all CGs using the last known fade-out duration
             // 告知管理器使用最后已知的淡出时长隐藏所有 CG
             CGManager.HideAllFullscreenCGs(lastKnownFadeOutDuration);
             currentActiveCgIdentifier = null;
             lastKnownFadeOutDuration = -1f; // Reset
        }
        // Case 3: The same clip remains active, or no clip is active and none was active before. Do nothing.
        // 情况 3：同一个片段保持活动，或者没有片段活动且之前也没有活动。不执行任何操作。
    }
}
