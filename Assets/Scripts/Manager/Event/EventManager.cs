// File: Scripts/Manager/Event/EventManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Needed for LINQ operations like FirstOrDefault

/// <summary>
/// Manages global event listening, triggering, timed events, and cancellation.
/// 管理全局事件监听、触发、定时事件及取消。
/// Assumes Singleton pattern.
/// 假设使用单例模式。
/// </summary>
public class EventManager : Singleton<EventManager>
{
    private Dictionary<Type, Action<object>> eventListeners = new Dictionary<Type, Action<object>>();

    // TimeEvent structure now includes a unique name for cancellation
    // 时间事件结构体现在包含唯一名称以便取消
    [System.Serializable]
    private struct TimeEvent
    {
        public string eventName;       // Unique name for identification and cancellation / 用于识别和取消的唯一名称
        public float triggerTime;      // Absolute trigger time (seconds) / 绝对触发时间（秒）
        public bool hasTriggered;      // If already triggered / 是否已被触发
        public bool isCancelled;       // If cancelled before triggering / 是否在触发前被取消
        public object eventData;       // Event data / 事件数据
        public Type eventType;         // Event Type / 事件类型
    }

    private float elapsedTime = 0f;
    private List<TimeEvent> timeEvents = new List<TimeEvent>();

    void Update()
    {
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            elapsedTime += Time.deltaTime;
            CheckTimeEvents();
        }
    }

    // --- Listener Management (No changes needed) ---
    public void AddListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (!eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = null;
        }
        eventListeners[eventType] += (obj) => {
            try { listener?.Invoke(obj as T); }
            catch (Exception ex) { Debug.LogError($"Error invoking listener for event {eventType.Name}: {ex.Message}\n{ex.StackTrace}"); }
        };
    }

    public void RemoveListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.ContainsKey(eventType))
        {
            Delegate[] invocationList = eventListeners[eventType]?.GetInvocationList();
            if (invocationList != null)
            {
                foreach (Delegate existingDelegate in invocationList)
                {
                    Action<object> action = existingDelegate as Action<object>;
                    if (action != null && action.Target == listener.Target && action.Method.ToString().Contains(listener.Method.ToString()))
                    {
                        eventListeners[eventType] -= action;
                        return;
                    }
                }
            }
        }
    }

    // --- Event Triggering (No changes needed) ---
    public void TriggerEvent<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.TryGetValue(eventType, out Action<object> thisEvent))
        {
            thisEvent?.Invoke(eventData);
        }
    }

    // --- Timed Event Management ---

    /// <summary>
    /// Registers a timed event that will trigger after a delay.
    /// 注册一个将在延迟后触发的定时事件。
    /// </summary>
    /// <param name="eventName">A unique name to identify this timed event for potential cancellation. / 用于识别此定时事件以供可能取消的唯一名称。</param>
    /// <param name="triggerDelaySeconds">Delay in seconds before triggering. / 触发前的延迟（秒）。</param>
    /// <param name="eventData">The event data object to trigger. / 要触发的事件数据对象。</param>
    public void RegisterTimeEvent<T>(string eventName, float triggerDelaySeconds, T eventData) where T : class
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("[EventManager] Cannot register timed event with an empty name!");
            return;
        }
        // Check if an event with this name already exists and hasn't triggered/cancelled
        // 检查是否已存在同名且未触发/取消的事件
        if (timeEvents.Any(te => te.eventName == eventName && !te.hasTriggered && !te.isCancelled))
        {
             Debug.LogWarning($"[EventManager] A non-triggered/non-cancelled timed event with name '{eventName}' already exists. Overwriting.");
             // Optionally remove the old one first
             // 可选：先移除旧的
             CancelTimeEvent(eventName); // Cancel existing before adding new
        }


        float absoluteTriggerTime = elapsedTime + triggerDelaySeconds;
        TimeEvent newEvent = new TimeEvent
        {
            eventName = eventName,
            triggerTime = absoluteTriggerTime,
            hasTriggered = false,
            isCancelled = false, // Initialize as not cancelled / 初始化为未取消
            eventData = eventData,
            eventType = typeof(T)
        };
        timeEvents.Add(newEvent);
        // Debug.Log($"[EventManager] Registered timed event: '{eventName}' to trigger at {absoluteTriggerTime:F2}s (Delay: {triggerDelaySeconds:F2}s)");
    }

    /// <summary>
    /// Checks and triggers any pending, non-cancelled timed events.
    /// 检查并触发任何待处理的、未取消的定时事件。
    /// </summary>
    private void CheckTimeEvents()
    {
        // Use List<TimeEvent> directly, modify by index carefully
        // 直接使用 List<TimeEvent>，小心地通过索引修改
        for (int i = 0; i < timeEvents.Count; i++) // Iterate forward to modify struct in place
        {
            TimeEvent evt = timeEvents[i]; // Get a copy of the struct

            // Check if not triggered, not cancelled, and time is up
            // 检查是否未触发、未取消且时间已到
            if (!evt.hasTriggered && !evt.isCancelled && elapsedTime >= evt.triggerTime)
            {
                // Debug.Log($"[EventManager] Triggering timed event (Elapsed: {elapsedTime:F2}s >= Trigger: {evt.triggerTime:F2}s): {evt.eventName}");
                TriggerEvent(evt.eventData);

                // Mark as triggered by updating the struct in the list
                // 通过更新列表中的结构体将其标记为已触发
                evt.hasTriggered = true;
                timeEvents[i] = evt; // Write the modified struct back to the list
            }
        }

        // Optional: Clean up triggered/cancelled events periodically to prevent list growth
        // 可选：定期清理已触发/取消的事件以防止列表增长
        // timeEvents.RemoveAll(te => te.hasTriggered || te.isCancelled);
    }

    /// <summary>
    /// Cancels a specific timed event by its unique name, preventing it from triggering.
    /// 通过唯一名称取消特定的定时事件，阻止其触发。
    /// </summary>
    /// <param name="eventName">The unique name of the timed event to cancel. / 要取消的定时事件的唯一名称。</param>
    /// <returns>True if an event was found and cancelled, false otherwise. / 如果找到并取消了事件，则为 true，否则为 false。</returns>
    public bool CancelTimeEvent(string eventName)
    {
        if (string.IsNullOrEmpty(eventName)) return false;

        bool cancelled = false;
        for (int i = 0; i < timeEvents.Count; i++)
        {
            TimeEvent evt = timeEvents[i]; // Get struct copy
            if (evt.eventName == eventName && !evt.hasTriggered && !evt.isCancelled)
            {
                evt.isCancelled = true;
                timeEvents[i] = evt; // Write modification back
                cancelled = true;
                Debug.Log($"[EventManager] Cancelled timed event: '{eventName}'");
                // break; // Assuming event names are unique, we can stop searching / 假设事件名称唯一，可以停止搜索
            }
        }
        if (!cancelled)
        {
             // Debug.LogWarning($"[EventManager] Could not find active timed event named '{eventName}' to cancel.");
        }
        return cancelled;
    }


    /// <summary>
    /// Resets the elapsed timer and clears all registered timed events.
    /// 重置经过的计时器并清除所有已注册的定时事件。
    /// </summary>
    public void ResetTimeEvents()
    {
        elapsedTime = 0f;
        int count = timeEvents.Count;
        timeEvents.Clear();
        // Debug.Log($"[EventManager] Timer reset and {count} timed events cleared.");
    }

    // --- Other methods (GetSeconds, RemoveAllListenersForType) remain the same ---
    public float GetSeconds() { return elapsedTime; }
    public void RemoveAllListenersForType(Type eventType) { eventListeners.Remove(eventType); }
    public void RemoveAllListenersForType<T>() where T : class { RemoveAllListenersForType(typeof(T)); }

    void OnDestroy() { ResetTimeEvents(); }
}
