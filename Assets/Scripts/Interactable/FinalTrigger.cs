using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// An interactable trigger for the final sequence of the game.
/// Plays the MC's sacrifice Timeline and triggers the ending check.
/// 游戏最终序列的可交互触发器。
/// 播放 MC 的牺牲 Timeline 并触发结局检查。
/// </summary>
public class FinalTrigger : Interactable
{
    [Header("Final Sequence Settings")]
    [Tooltip("PlayableDirector for the MC's final sacrifice sequence.\nMC 最终牺牲序列的 PlayableDirector。")]
    [SerializeField] private PlayableDirector finalSequenceDirector;

    private bool interactionComplete = false;

    protected override void Start()
    {
        base.Start();
        interactionComplete = false;
    }

    public override void Interact()
    {
        if (interactionComplete) return;

        // Check if this is the correct stage (target is "Final")
        // 检查这是否是正确的阶段（目标是 "Final"）
        if (ProgressManager.Instance == null) {
             Debug.LogError("[FinalTrigger] ProgressManager not found!");
             return;
        }
        if (ProgressManager.Instance.CurrentMainTarget != "Final")
        {
            Debug.Log($"[FinalTrigger] Interaction attempted, but current target is '{ProgressManager.Instance.CurrentMainTarget}', not 'Final'.");
            // Optionally show dialogue indicating it's not time yet
            // 可选地显示对话，指示时机未到
             if (DialogueManager.Instance != null) DialogueManager.Instance.ShowBlockingDialogue("It's not time yet...");
            return;
        }

        Debug.Log("[FinalTrigger] Final interaction triggered.");
        interactionComplete = true; // Prevent re-interaction / 阻止重复交互

        // Play the final Timeline
        // 播放最终 Timeline
        if (finalSequenceDirector != null)
        {
            Debug.Log($"[FinalTrigger] Playing final sequence director: {finalSequenceDirector.name}");
            finalSequenceDirector.Play();
            // Add a listener to the director's 'stopped' event to trigger the ending check
            // 为导演的 'stopped' 事件添加监听器以触发结局检查
            finalSequenceDirector.stopped += OnFinalSequenceStopped;
        }
        else
        {
            Debug.LogError("[FinalTrigger] Final Sequence Director is not assigned! Cannot play final sequence.", this);
            // Fallback: Trigger ending check immediately if no Timeline
            // 回退：如果没有 Timeline，则立即触发结局检查
            TriggerEndingCheck();
        }

        // Disable further interaction with this trigger
        // 禁用与此触发器的进一步交互
        // (Could also be done via Timeline Activation Track)
        // （也可以通过 Timeline Activation Track 完成）
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        HideMarker(); // Hide interaction marker / 隐藏交互标记
    }

    /// <summary>
    /// Called when the finalSequenceDirector finishes playing.
    /// 当 finalSequenceDirector 播放完成时调用。
    /// </summary>
    /// <param name="director">The director that stopped. / 停止的导演。</param>
    private void OnFinalSequenceStopped(PlayableDirector director)
    {
        Debug.Log("[FinalTrigger] Final sequence Timeline stopped.");
        // Unsubscribe to prevent multiple calls if the timeline is replayed somehow
        // 取消订阅以防止在时间轴以某种方式重播时多次调用
        if (director != null) director.stopped -= OnFinalSequenceStopped;

        TriggerEndingCheck();
    }

    /// <summary>
    /// Triggers the event to request the EndingManager to check conditions.
    /// 触发事件以请求 EndingManager 检查条件。
    /// </summary>
    private void TriggerEndingCheck()
    {
        Debug.Log("[FinalTrigger] Triggering EndingCheckRequestedEvent.");
        if (EventManager.Instance != null)
        {
             EventManager.Instance.TriggerEvent(new EndingCheckRequestedEvent());
        }
        else
        {
            Debug.LogError("[FinalTrigger] EventManager not found! Cannot trigger ending check.");
        }
        // Destroy self after triggering? Optional.
        // 触发后销毁自身？可选。
        // Destroy(gameObject, 0.1f);
    }

    public override string GetDialogue()
    {
        // Check ProgressManager exists and is in correct stage
        // 检查 ProgressManager 是否存在且处于正确阶段
        if (!interactionComplete && ProgressManager.Instance != null && ProgressManager.Instance.CurrentMainTarget == "Final")
        {
            return "按 E 结束这一切 (Press E to end this)"; // Final prompt / 最终提示
        }
        return ""; // No prompt otherwise / 否则无提示
    }

    // Ensure listener is removed if the object is destroyed before the timeline finishes
    // 确保如果在时间轴完成之前对象被销毁，则移除监听器
    void OnDestroy()
    {
        if (finalSequenceDirector != null)
        {
            // Attempt to remove listener safely
            // 尝试安全地移除监听器
            try { finalSequenceDirector.stopped -= OnFinalSequenceStopped; } catch {}
        }
    }
}
