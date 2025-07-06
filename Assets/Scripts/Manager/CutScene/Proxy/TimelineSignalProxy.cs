using UnityEngine;
using UnityEngine.Playables;
// using YourEventsNamespace; // If your events are in a namespace

/// <summary>
/// Acts as a local receiver for Timeline signals within a specific scene.
/// It then calls the appropriate methods on persistent Singleton managers OR triggers global events.
/// Attach this component to the GameObject that the Timeline's Signal Tracks are bound to.
/// 作为特定场景内 Timeline 信号的本地接收器。
/// 然后它会调用持久性 Singleton 管理器上的相应方法 或 触发全局事件。
/// 将此组件附加到 Timeline 的 Signal Track 所绑定的 GameObject 上。
/// </summary>
public class TimelineSignalProxy : MonoBehaviour
{
    [Header("Dialogue Settings (Optional)")]
    [Tooltip("Default dialogue text to show via signal.")]
    [TextArea]
    public string dialogueToShow = "Default Dialogue...";

    // --- Public methods to be called by Signal Receiver ---

    // --- Game State Signals ---
    public void TriggerEnterCutsceneState()
    {
        // Debug.Log($"[TimelineSignalProxy] Received signal to ENTER Cutscene State on {gameObject.name}. Calling GameRunManager.");
        GameRunManager.Instance?.EnterCutsceneState();
    }
    public void TriggerExitCutsceneState()
    {
        // Debug.Log($"[TimelineSignalProxy] Received signal to EXIT Cutscene State on {gameObject.name}. Calling GameRunManager.");
        GameRunManager.Instance?.ExitCutsceneState();
    }

    // --- Dialogue Signals ---
    /// <summary>
    /// Called by a Signal Emitter to show a brief notification message.
    /// 由 Signal Emitter 调用以显示简短的通知消息。
    /// </summary>
    /// <param name="message">The message to display. / 要显示的消息。</param>
    public void TriggerShowDialogue(string message) // Renamed parameter for clarity, still called by Timeline signal
    {
         // Debug.Log($"[TimelineSignalProxy] Received signal to show notification on {gameObject.name}: '{message}'. Calling DialogueManager.");
         // --- FIX: Call ShowNotification instead of ShowDialogue ---
         DialogueManager.Instance?.ShowNotification(message);
         // --- END FIX ---
    }
    public void TriggerShowDefaultDialogue() { TriggerShowDialogue(dialogueToShow); } // This will now show a notification

    // This method might be deprecated if using custom DialogueTrack exclusively
    // 如果专门使用自定义 DialogueTrack，此方法可能已弃用
    public void TriggerShowPausableDialogue(string message)
    {
        PlayableDirector director = GetComponent<PlayableDirector>();
        if (director == null) { Debug.LogError($"[TimelineSignalProxy] PlayableDirector not found on {gameObject.name}!"); return; }
        // Debug.Log($"[TimelineSignalProxy] Received signal to show PAUSABLE dialogue: '{message}'. Triggering ShowPausableDialogueRequestedEvent.");
        // EventManager.Instance?.TriggerEvent(new ShowPausableDialogueRequestedEvent(message, director));
    }

    // --- Gameplay Event Signals ---
    public void TriggerOpenDoorEventGlobally(string doorToOpenID)
    {
        // Debug.Log($"[TimelineSignalProxy] Received signal to open door: {doorToOpenID}. Triggering global OpenSpecificDoorEvent.");
        EventManager.Instance?.TriggerEvent(new OpenSpecificDoorEvent(doorToOpenID));
    }
    public void TriggerSimpleActionEventGlobally()
    {
        // Debug.Log($"[TimelineSignalProxy] Received simple action signal. Triggering global MyTimelineActionEvent.");
        EventManager.Instance?.TriggerEvent(new MyTimelineActionEvent());
    }

    // --- CG Event Signals ---
    public void TriggerShowCGGlobally(string identifierAndDuration)
    {
        string identifier = identifierAndDuration; float duration = -1f;
        if (identifierAndDuration.Contains(":"))
        {
            string[] parts = identifierAndDuration.Split(':');
            if (parts.Length == 2)
            {
                identifier = parts[0].Trim();
                if (!float.TryParse(parts[1].Trim(), out duration)) { duration = -1f; }
            } else { identifier = identifierAndDuration; } // Fallback
        }
        // Debug.Log($"[TimelineSignalProxy] Received signal to show GLOBAL CG. Identifier: '{identifier}', Duration: {duration}. Triggering ShowCGRequestedEvent.");
        EventManager.Instance?.TriggerEvent(new ShowCGRequestedEvent(identifier, duration));
    }
    public void TriggerHideAllCGsGlobally(float duration = -1f)
    {
        // Debug.Log($"[TimelineSignalProxy] Received signal to hide all GLOBAL CGs. Duration: {duration}. Triggering HideAllCGsRequestedEvent.");
        EventManager.Instance?.TriggerEvent(new HideAllCGsRequestedEvent(duration));
    }

    // --- Cutscene Sequence Completion Signal ---
    public void TriggerCutsceneSequenceCompleted(string sequenceIdentifier)
    {
        if (string.IsNullOrEmpty(sequenceIdentifier)) { /* Warning Log */ return; }
        // Debug.Log($"[TimelineSignalProxy] Received signal that sequence '{sequenceIdentifier}' completed. Triggering global CutsceneSequenceCompletedEvent.");
        EventManager.Instance?.TriggerEvent(new CutsceneSequenceCompletedEvent(sequenceIdentifier));
    }
}
