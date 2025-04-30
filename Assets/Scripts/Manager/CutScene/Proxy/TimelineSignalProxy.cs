using UnityEngine;
using UnityEngine.Playables;
// 确保你引用了包含新事件定义的命名空间（如果需要）
// using YourEventsNamespace; // If your events are in a namespace

/// <summary>
/// Acts as a local receiver for Timeline signals within a specific scene.
/// 作为特定场景内 Timeline 信号的本地接收器。
/// It then calls the appropriate methods on persistent Singleton managers OR triggers global events.
/// 然后它会调用持久性 Singleton 管理器上的相应方法 或 触发全局事件。
/// Attach this component to the GameObject that the Timeline's Signal Tracks are bound to.
/// 将此组件附加到 Timeline 的 Signal Track 所绑定的 GameObject 上。
/// </summary>
public class TimelineSignalProxy : MonoBehaviour
{
    [Header("Dialogue Settings (Optional)")]
    [Tooltip("Default dialogue text to show via signal.")]
    [TextArea]
    public string dialogueToShow = "Default Dialogue...";

    // --- Public methods to be called by Signal Receiver ---

    /// <summary>
    /// Called by a Signal Emitter for starting a cutscene state. (Calls Manager directly)
    /// 由用于启动过场动画状态的 Signal Emitter 调用。(直接调用管理器)
    /// </summary>
    public void TriggerEnterCutsceneState()
    {
        Debug.Log($"[TimelineSignalProxy] Received signal to ENTER Cutscene State on {gameObject.name}. Calling GameRunManager.");
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.EnterCutsceneState();
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] GameRunManager.Instance is null! Cannot enter cutscene state.");
        }
    }

    /// <summary>
    /// Called by a Signal Emitter for exiting a cutscene state. (Calls Manager directly)
    /// 由用于退出过场动画状态的 Signal Emitter 调用。(直接调用管理器)
    /// </summary>
    public void TriggerExitCutsceneState()
    {
        Debug.Log($"[TimelineSignalProxy] Received signal to EXIT Cutscene State on {gameObject.name}. Calling GameRunManager.");
        if (GameRunManager.Instance != null)
        {
            GameRunManager.Instance.ExitCutsceneState();
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] GameRunManager.Instance is null! Cannot exit cutscene state.");
        }
    }

    /// <summary>
    /// Called by a Signal Emitter to show a specific dialogue line. (Calls Manager directly)
    /// 由用于显示特定对话行的 Signal Emitter 调用。(直接调用管理器)
    /// </summary>
    /// <param name="message">The dialogue message to display. / 要显示的对话消息。</param>
    public void TriggerShowDialogue(string message)
    {
         Debug.Log($"[TimelineSignalProxy] Received signal to show dialogue on {gameObject.name}: '{message}'. Calling DialogueManager.");
         if (DialogueManager.Instance != null)
         {
             DialogueManager.Instance.ShowDialogue(message);
         }
         else
         {
             Debug.LogError("[TimelineSignalProxy] DialogueManager.Instance is null! Cannot show dialogue.");
         }
    }

     /// <summary>
     /// Called by a Signal Emitter to show the default dialogue configured in the Inspector. (Calls Manager directly)
     /// 由用于显示在 Inspector 中配置的默认对话的 Signal Emitter 调用。(直接调用管理器)
     /// </summary>
     public void TriggerShowDefaultDialogue()
     {
          TriggerShowDialogue(dialogueToShow);
     }

    // --- [T] NEW Methods to Trigger Global Events ---
    // --- [T] 新增：用于触发全局事件的方法 ---

    #region Global general methods
    /// <summary>
    /// 由 Signal Emitter 调用，用于触发全局的“打开特定门”事件。
    /// Called by a Signal Emitter to trigger the global 'Open Specific Door' event.
    /// </summary>
    /// <param name="doorToOpenID">需要打开的门的唯一ID (The unique ID of the door to open)</param>
    public void TriggerOpenDoorEventGlobally(string doorToOpenID)
    {
        Debug.Log($"[TimelineSignalProxy] Received signal to open door: {doorToOpenID}. Triggering global OpenSpecificDoorEvent.");
        if (EventManager.Instance != null)
        {
            // 触发全局事件，并传递门的ID
            // Trigger the global event, passing the door ID
            EventManager.Instance.TriggerEvent(new OpenSpecificDoorEvent(doorToOpenID));
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] EventManager not found! Cannot trigger OpenSpecificDoorEvent.");
        }
    }

    /// <summary>
    /// 由 Signal Emitter 调用，用于触发一个简单的、无数据的全局事件。
    /// Called by a Signal Emitter to trigger a simple, data-less global event.
    /// </summary>
    public void TriggerSimpleActionEventGlobally()
    {
         Debug.Log($"[TimelineSignalProxy] Received simple action signal. Triggering global MyTimelineActionEvent.");
         if (EventManager.Instance != null)
         {
             // 触发简单的全局事件
             // Trigger the simple global event
             EventManager.Instance.TriggerEvent(new MyTimelineActionEvent());
         }
         else
         {
              Debug.LogError("[TimelineSignalProxy] EventManager not found! Cannot trigger MyTimelineActionEvent.");
         }
    }

    /// <summary>
    /// 由 Signal Emitter 调用，用于触发全局的“显示可暂停对话”事件。
    /// Called by a Signal Emitter to trigger the global 'Show Pausable Dialogue' event.
    /// </summary>
    /// <param name="message">要显示的单行对话文本 (The single line of dialogue to display)</param>
    public void TriggerShowPausableDialogue(string message)
    {
        // 获取附加在同一个 GameObject 上的 PlayableDirector 组件
        // Get the PlayableDirector component attached to the same GameObject
        PlayableDirector director = GetComponent<PlayableDirector>();

        if (director == null)
        {
            Debug.LogError($"[TimelineSignalProxy] PlayableDirector component not found on {gameObject.name}! Cannot trigger pausable dialogue.");
            return;
        }

        Debug.Log($"[TimelineSignalProxy] Received signal to show PAUSABLE dialogue: '{message}'. Triggering ShowPausableDialogueRequestedEvent.");
        if (EventManager.Instance != null)
        {
            // 触发全局事件，传递对话文本和需要暂停的 Director
            // Trigger the global event, passing the dialogue text and the director to pause
            EventManager.Instance.TriggerEvent(new ShowPausableDialogueRequestedEvent(message, director));
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] EventManager not found! Cannot trigger ShowPausableDialogueRequestedEvent.");
        }
    }
    #endregion

    // --- [CutsceneUIManager] 触发全局 CG 事件 ---
    // --- [CutsceneUIManager] NEW Methods: Trigger Global CG Events ---
    #region Global CG event
    /// <summary>
    /// 由 Signal Emitter 调用，用于触发全局的“显示/切换 CG”事件。
    /// Called by a Signal Emitter to trigger the global 'Show/Switch CG' event.
    /// </summary>
    /// <param name="identifierAndDuration">一个组合字符串，格式为 "CgIdentifier:FadeDuration"。例如 "Bedroom_Night:-1" 或 "Classroom_Day:0.8"。如果省略冒号和时长，则时长使用默认值-1。</param>
    /// <remarks>
    /// 使用组合字符串是因为 Signal Emitter 对直接传递多个不同类型参数的支持有限。
    /// Using a combined string because Signal Emitter has limited support for passing multiple different parameter types directly.
    /// </remarks>
    public void TriggerShowCGGlobally(string identifierAndDuration)
    {
        string identifier = identifierAndDuration;
        float duration = -1f; // 默认时长 (Default duration)

        // 解析字符串以分离 ID 和时长
        // Parse the string to separate ID and duration
        if (identifierAndDuration.Contains(":"))
        {
            string[] parts = identifierAndDuration.Split(':');
            if (parts.Length == 2)
            {
                identifier = parts[0].Trim();
                if (float.TryParse(parts[1].Trim(), out float parsedDuration))
                {
                    duration = parsedDuration;
                } else {
                    Debug.LogWarning($"[TimelineSignalProxy] Could not parse duration from '{identifierAndDuration}'. Using default duration.");
                }
            } else {
                 Debug.LogWarning($"[TimelineSignalProxy] Invalid format for identifierAndDuration: '{identifierAndDuration}'. Expected 'Identifier:Duration'. Using full string as identifier and default duration.");
                 identifier = identifierAndDuration; // Fallback: use the whole string as ID
            }
        }

        Debug.Log($"[TimelineSignalProxy] Received signal to show GLOBAL CG. Identifier: '{identifier}', Duration: {duration}. Triggering ShowCGRequestedEvent.");
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(new ShowCGRequestedEvent(identifier, duration));
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] EventManager not found! Cannot trigger ShowCGRequestedEvent.");
        }
    }

    /// <summary>
    /// 由 Signal Emitter 调用，用于触发全局的“隐藏所有 CG”事件。
    /// Called by a Signal Emitter to trigger the global 'Hide All CGs' event.
    /// </summary>
    /// <param name="duration">淡出时长（秒）。负数表示使用默认值。(Fade duration in seconds. Negative value means use default.)</param>
    public void TriggerHideAllCGsGlobally(float duration = -1f) // 参数可以直接从 Emitter 传入 float
    {
        Debug.Log($"[TimelineSignalProxy] Received signal to hide all GLOBAL CGs. Duration: {duration}. Triggering HideAllCGsRequestedEvent.");
        if (EventManager.Instance != null)
        {
            EventManager.Instance.TriggerEvent(new HideAllCGsRequestedEvent(duration));
        }
        else
        {
            Debug.LogError("[TimelineSignalProxy] EventManager not found! Cannot trigger HideAllCGsRequestedEvent.");
        }
    }
    #endregion
}
