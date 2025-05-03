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
    public string SpeakerName { get; set; }
    public string DialogueText { get; set; } // This should be set by CreatePlayable from the Asset
    public bool PauseTimelineUntilFinished { get; set; }
    public DialogueManager DialogueManager { get; set; }

    private PlayableGraph graph;
    private bool pauseScheduled = false;
    private bool interactionStarted = false;
    private bool firstFrameProcessed = false; // Use a flag specific to PrepareFrame

    public override void OnPlayableCreate(Playable playable)
    {
        graph = playable.GetGraph();
    }

    // Reset flags when the behaviour starts playing within the graph's lifecycle
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        firstFrameProcessed = false; // Reset for this activation
        interactionStarted = false;
        pauseScheduled = false;
        // Debug.Log($"[DialogueBehaviour OnBehaviourPlay] Resetting flags for clip: '{DialogueText?.Substring(0, Mathf.Min(DialogueText.Length, 15))}...'");
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // ... (Existing OnBehaviourPause logic for cleanup and hiding) ...
        // Ensure flags are reset here too if the clip pauses mid-way
        firstFrameProcessed = false;
        interactionStarted = false;
        pauseScheduled = false;
    }

    public override void OnGraphStart(Playable playable)
    {
        // Reset flags at the very start of the graph
        firstFrameProcessed = false;
        interactionStarted = false;
        pauseScheduled = false;
        UnsubscribeDialogueComplete();
    }

    public override void OnGraphStop(Playable playable)
    {
        // ... (Existing OnGraphStop logic) ...
        firstFrameProcessed = false;
        interactionStarted = false;
        pauseScheduled = false;
    }


    // Called every frame for active/blending clips BEFORE ProcessFrame
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (DialogueManager == null) return;

        // Check if this is the first frame this specific behaviour instance is processed
        // in its current activation cycle AND has significant weight.
        if (!firstFrameProcessed && info.weight > 0.5f) // Use a higher threshold to be sure it's dominant
        {
            // --- DEBUG LOG ---
            Debug.Log($"[DialogueBehaviour PrepareFrame] First frame processing for clip. Text: '{DialogueText}', RequiresInput: {PauseTimelineUntilFinished}, Weight: {info.weight}");
            // --- END DEBUG LOG ---

            firstFrameProcessed = true; // Mark as processed for this activation
            interactionStarted = true; // Mark that we initiated a dialogue interaction

            // Call the DialogueManager to show the text
            DialogueManager.ShowTimelineDialogue(SpeakerName, DialogueText, PauseTimelineUntilFinished);

            if (PauseTimelineUntilFinished)
            {
                SubscribeDialogueComplete(); // Subscribe before scheduling pause
                pauseScheduled = true;       // Schedule pause for ProcessFrame
            }
            else
            {
                UnsubscribeDialogueComplete(); // Ensure no listener if not pausing
            }
        }
        // else if (info.weight > 0.01f) // Log subsequent frames if needed (can be spammy)
        // {
        //     Debug.Log($"[DialogueBehaviour PrepareFrame] Subsequent frame processing for clip. Text: '{DialogueText}', Weight: {info.weight}");
        // }
    }


    // Called every frame for active/blending clips AFTER PrepareFrame
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // Execute the pause scheduled in PrepareFrame
        if (pauseScheduled)
        {
            pauseScheduled = false; // Consume the schedule
            if (graph.IsValid() && graph.IsPlaying())
            {
                // Debug.Log("[DialogueBehaviour ProcessFrame] Pausing Timeline graph.");
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
