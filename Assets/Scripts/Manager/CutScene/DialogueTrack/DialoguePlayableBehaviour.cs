using UnityEngine;
using UnityEngine.Playables;
using System;

/// <summary>
/// The runtime logic for a dialogue clip, adapted for the modified DialogueManager.
/// It calls DialogueManager.ShowTimelineDialogue and uses the onTimelineDialogueComplete event.
/// Includes logic to explicitly hide dialogue when the clip ends naturally.
/// 对话片段的运行时逻辑，已适配修改后的 DialogueManager。
/// 它调用 DialogueManager.ShowTimelineDialogue 并使用 onTimelineDialogueComplete 事件。
/// 包含在片段自然结束时显式隐藏对话的逻辑。
/// </summary>
public class DialoguePlayableBehaviour : PlayableBehaviour
{
    // --- Data received from DialoguePlayableAsset ---
    public string SpeakerName { get; set; }
    public string DialogueText { get; set; }
    public bool PauseTimelineUntilFinished { get; set; }

    // --- Runtime References ---
    public DialogueManager DialogueManager { get; set; } // Set by the DialogueTrackMixerBehaviour

    private PlayableGraph graph;
    private bool pauseScheduled = false;
    private bool isFirstFrame = true;
    private bool interactionStarted = false; // Track if we initiated the dialogue

    public override void OnPlayableCreate(Playable playable)
    {
        graph = playable.GetGraph();
    }

    public override void OnGraphStart(Playable playable)
    {
        isFirstFrame = true;
        interactionStarted = false;
        UnsubscribeDialogueComplete(); // Ensure clean state on graph start
    }

    public override void OnGraphStop(Playable playable)
    {
        // If the graph stops while our dialogue might be active, ensure cleanup.
        // 如果图在我们对话可能激活时停止，确保清理。
        if (interactionStarted)
        {
             // Optionally hide dialogue immediately when graph stops
             // 可选：当图停止时立即隐藏对话
             // DialogueManager?.HideDialogue();
             CleanupDialogue(); // Unsubscribe event listener
        }
        isFirstFrame = true;
        interactionStarted = false;
        pauseScheduled = false;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        isFirstFrame = true;
        interactionStarted = false;
    }

    /// <summary>
    /// Called when the behaviour becomes inactive (playable time leaves the clip duration or director pauses/stops).
    /// Now includes logic to hide the dialogue if the clip ends naturally.
    /// 当行为变为非活动状态时调用（播放时间离开片段持续时间或导演暂停/停止）。
    /// 现在包含在片段自然结束时隐藏对话的逻辑。
    /// </summary>
    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // Check if the interaction was started by this behaviour instance
        // 检查交互是否由此行为实例启动
        if (interactionStarted)
        {
            // --- ADDED LOGIC ---
            // Determine if the pause is because the clip reached its end while the graph is still playing.
            // 判断暂停是否因为在图仍在播放时片段到达了其末尾。
            var time = playable.GetTime();
            var duration = playable.GetDuration();
            // Consider it ended naturally if time is at or very close to duration, and the director wasn't externally paused.
            // 如果时间等于或非常接近持续时间，并且导演没有被外部暂停，则认为其自然结束。
            // info.effectivePlayState gives the state *after* this frame's evaluation, so PlayState.Playing is okay here.
            bool clipEndedNaturally = (time >= duration || Mathf.Approximately((float)time, (float)duration)) && info.effectivePlayState == PlayState.Playing;

            if (clipEndedNaturally)
            {
                Debug.Log($"[DialoguePlayableBehaviour] Clip ended naturally for '{DialogueText}'. Forcing HideDialogue.");
                // If the clip finished its duration, explicitly tell the DialogueManager to hide.
                // 如果片段完成了其持续时间，明确告知 DialogueManager 隐藏。
                DialogueManager?.HideDialogue();
                // Note: HideDialogue() in the modified DialogueManager should trigger onTimelineDialogueComplete if needed,
                // which would call ResumeTimeline, which calls Unsubscribe. So calling Unsubscribe here might be redundant
                // but safe.
                // 注意：修改后的 DialogueManager 中的 HideDialogue() 应在需要时触发 onTimelineDialogueComplete，
                // 这会调用 ResumeTimeline，而 ResumeTimeline 会调用 Unsubscribe。所以在此处调用 Unsubscribe 可能是多余的，但安全。
            }
            else
            {
                 Debug.Log($"[DialoguePlayableBehaviour] Paused (Not natural end). Clip time: {time}/{duration}, EffectiveState: {info.effectivePlayState}. Only cleaning up listeners.");
                 // If paused for other reasons (director paused externally), just clean up listeners.
                 // 如果因其他原因暂停（导演从外部暂停），则仅清理监听器。
            }
             // --- END ADDED LOGIC ---

            // Always clean up the event listener when the behaviour pauses after interaction started.
            // 在交互开始后行为暂停时，始终清理事件监听器。
            CleanupDialogue();
        }

        // Reset flags for the next time the behaviour might become active
        // 为行为下次可能变为活动状态重置标志
        isFirstFrame = true;
        interactionStarted = false;
        pauseScheduled = false;
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (DialogueManager == null) return;

        if (isFirstFrame && info.weight > 0f)
        {
            isFirstFrame = false;
            interactionStarted = true; // Mark that we started the dialogue

            // Call the dedicated Timeline method on DialogueManager
            DialogueManager.ShowTimelineDialogue(SpeakerName, DialogueText, PauseTimelineUntilFinished);

            if (PauseTimelineUntilFinished)
            {
                // Subscribe *before* scheduling pause
                SubscribeDialogueComplete();
                pauseScheduled = true;
            }
            else
            {
                UnsubscribeDialogueComplete(); // Ensure no lingering subscription if not pausing
            }
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (pauseScheduled)
        {
            pauseScheduled = false;
            if (graph.IsValid() && graph.IsPlaying())
            {
                Debug.Log("[DialoguePlayableBehaviour] Pausing Timeline graph.");
                graph.Stop();
            }
        }
    }

    /// <summary>
    /// Cleans up the dialogue state: primarily unsubscribes from events.
    /// 清理对话状态：主要是取消订阅事件。
    /// </summary>
    private void CleanupDialogue()
    {
        UnsubscribeDialogueComplete();
        // We now handle explicit hiding in OnBehaviourPause when the clip ends naturally.
        // 我们现在在 OnBehaviourPause 中处理片段自然结束时的显式隐藏。
        // interactionStarted = false; // Resetting interactionStarted is handled in OnPause/OnStop/Resume
    }

    /// <summary>
    /// Method called by DialogueManager.onTimelineDialogueComplete when the dialogue ends via player input/skip.
    /// 当对话通过玩家输入/跳过结束时，由 DialogueManager.onTimelineDialogueComplete 调用。
    /// </summary>
    private void ResumeTimeline()
    {
        // Unsubscribe FIRST to prevent potential race conditions or multiple calls
        // 首先取消订阅以防止潜在的竞争条件或多次调用
        UnsubscribeDialogueComplete();

        // Resume the graph if it's valid and was paused
        if (graph.IsValid() && !graph.IsPlaying())
        {
            Debug.Log("[DialoguePlayableBehaviour] Resuming Timeline graph via ResumeTimeline callback.");
            graph.Play();
        } else if (graph.IsValid() && graph.IsPlaying()) {
             Debug.LogWarning("[DialoguePlayableBehaviour] ResumeTimeline called, but graph is already playing.");
        } else {
             Debug.LogWarning("[DialoguePlayableBehaviour] ResumeTimeline called, but graph is not valid.");
        }
        interactionStarted = false; // Dialogue interaction is now complete
    }

    /// <summary>
    /// Safely subscribes to the DialogueManager's completion event.
    /// 安全地订阅 DialogueManager 的完成事件。
    /// </summary>
    private void SubscribeDialogueComplete()
    {
         if (DialogueManager != null)
         {
             // Remove first to prevent double subscription
             // 先移除以防止重复订阅
             DialogueManager.onTimelineDialogueComplete -= ResumeTimeline;
             DialogueManager.onTimelineDialogueComplete += ResumeTimeline;
             Debug.Log("[DialoguePlayableBehaviour] Subscribed to onTimelineDialogueComplete.");
         }
    }


    /// <summary>
    /// Safely unsubscribes from the DialogueManager's completion event.
    /// 安全地取消订阅 DialogueManager 的完成事件。
    /// </summary>
    private void UnsubscribeDialogueComplete()
    {
        if (DialogueManager != null)
        {
            DialogueManager.onTimelineDialogueComplete -= ResumeTimeline;
            // Optional: Add a log here if needed for debugging unsubscribe calls
            // 可选：如果需要调试取消订阅调用，可在此处添加日志
            // Debug.Log("[DialoguePlayableBehaviour] Attempted to unsubscribe from onTimelineDialogueComplete.");
        }
    }
}
