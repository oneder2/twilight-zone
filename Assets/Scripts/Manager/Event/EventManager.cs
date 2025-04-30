// File: Scripts/Manager/Event/EventManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages global event listening, triggering, and timed events.
/// 管理全局事件监听、触发和定时事件。
/// Assumes Singleton pattern.
/// 假设使用单例模式。
/// </summary>
public class EventManager : Singleton<EventManager>
{
    // The dictionary which store event type and listener
    // 存储事件类型和监听器的字典
    private Dictionary<Type, Action<object>> eventListeners = new Dictionary<Type, Action<object>>();

    // Structor of Time event
    // 时间事件的结构体
    [System.Serializable]
    private struct TimeEvent
    {
        public string eventName;       // Event Name(To debug or marking) // 事件名称（用于调试或标记）
        public float triggerTime;      // Trigger time(second) // 触发时间（秒） - Changed to float for precision // 改为 float 以提高精度
        public bool hasTriggered;      // if has been triggered // 是否已被触发
        public object eventData;       // Event data when triggered // 触发时的事件数据
        public Type eventType;         // Event Type // 事件类型
    }

    private float elapsedTime = 0f; // 累计时间（秒）(Accumulated time in seconds)
    private List<TimeEvent> timeEvents = new List<TimeEvent>(); // 存储所有时间事件 (Stores all timed events)

    void Update()
    {
        // Only advance time and check events if the game is playing
        // 仅当游戏正在进行时才推进时间和检查事件
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            elapsedTime += Time.deltaTime; // 累加时间 (Accumulate time)
            CheckTimeEvents(); // 检查时间事件 (Check timed events)
        }
    }


    // 注册监听器 (Register listener)
    public void AddListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (!eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = null;
        }
        // Use a wrapper to handle potential null listeners during invocation
        // 使用包装器来处理调用期间潜在的空监听器
        eventListeners[eventType] += (obj) => {
            try {
                listener?.Invoke(obj as T); // Null-conditional invocation // 空条件调用
            } catch (Exception ex) {
                Debug.LogError($"Error invoking listener for event {eventType.Name}: {ex.Message}\n{ex.StackTrace}");
            }
        };
    }

    // 移除监听器 (Remove listener)
    public void RemoveListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.ContainsKey(eventType))
        {
            // Find the specific wrapper action to remove (more complex but necessary for correct removal)
            // 查找要移除的特定包装器操作（更复杂但对于正确移除是必需的）
            // This basic removal might not work reliably with lambdas if the exact lambda instance isn't passed.
            // 如果没有传递确切的 lambda 实例，这种基本移除可能无法可靠地与 lambda 一起工作。
            // A more robust system might store Action<object> directly or use unique IDs.
            // 更健壮的系统可能会直接存储 Action<object> 或使用唯一 ID。
            // For now, we assume this basic removal is sufficient for the project's needs.
            // 目前，我们假设这种基本移除足以满足项目需求。
             Delegate[] invocationList = eventListeners[eventType]?.GetInvocationList();
             if (invocationList != null)
             {
                 foreach (Delegate existingDelegate in invocationList)
                 {
                     Action<object> action = existingDelegate as Action<object>;
                     // Attempt to find the target method within the lambda
                     // 尝试在 lambda 中查找目标方法
                     if (action != null && action.Target == listener.Target && action.Method.ToString().Contains(listener.Method.ToString())) // Heuristic check // 启发式检查
                     {
                         eventListeners[eventType] -= action;
                         // Debug.Log($"Removed listener for {eventType.Name}"); // Verbose // 冗余
                         return; // Assume only one instance per listener method // 假设每个监听器方法只有一个实例
                     }
                 }
             }
             // Fallback if specific delegate not found (might remove wrong one if multiple identical lambdas registered)
             // 如果未找到特定委托则回退（如果注册了多个相同的 lambda，可能会移除错误的委托）
             // eventListeners[eventType] -= (obj) => listener(obj as T); // Less reliable // 不太可靠
        }
    }


    // 触发即时事件 (Trigger immediate event)
    public void TriggerEvent<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.TryGetValue(eventType, out Action<object> thisEvent)) // Use TryGetValue for safety // 使用 TryGetValue 以确保安全
        {
            // Invoke safely, the wrapper in AddListener handles individual listener errors
            // 安全调用，AddListener 中的包装器处理单个监听器错误
            thisEvent?.Invoke(eventData);
            // Debug.Log($"Triggered event: {eventType.Name}"); // Verbose // 冗余
        }
    }

    // 注册定时事件 (Register timed event)
    // Changed triggerTime parameter to float for flexibility
    // 将 triggerTime 参数更改为 float 以提高灵活性
    public void RegisterTimeEvent<T>(string eventName, float triggerDelaySeconds, T eventData) where T : class
    {
        // Calculate absolute trigger time based on current elapsed time
        // 根据当前经过时间计算绝对触发时间
        float absoluteTriggerTime = elapsedTime + triggerDelaySeconds;

        TimeEvent newEvent = new TimeEvent
        {
            eventName = eventName,
            triggerTime = absoluteTriggerTime, // Store absolute time // 存储绝对时间
            hasTriggered = false,
            eventData = eventData,
            eventType = typeof(T)
        };
        timeEvents.Add(newEvent);
        Debug.Log($"[EventManager] Registered timed event: '{eventName}' to trigger at {absoluteTriggerTime:F2} seconds (Delay: {triggerDelaySeconds:F2}s)");
    }

    // 检查并触发时间事件 (Check and trigger timed events)
    private void CheckTimeEvents()
    {
        // Iterate backwards to allow safe removal if needed (though we reset flags now)
        // 向后迭代以便在需要时安全移除（尽管我们现在重置标志）
        for (int i = timeEvents.Count - 1; i >= 0; i--)
        {
            // Use a temporary variable to avoid issues if the list is modified during iteration (safer)
            // 使用临时变量以避免在迭代期间修改列表时出现问题（更安全）
            TimeEvent evt = timeEvents[i];
            if (!evt.hasTriggered && elapsedTime >= evt.triggerTime)
            {
                Debug.Log($"[EventManager] Triggering timed event (Elapsed: {elapsedTime:F2}s >= Trigger: {evt.triggerTime:F2}s): {evt.eventName}");
                TriggerEvent(evt.eventData); // Use the main TriggerEvent method // 使用主 TriggerEvent 方法
                // Mark as triggered
                // 标记为已触发
                // Create a new struct instance to update the list element immutably
                // 创建一个新的结构体实例以不可变地更新列表元素
                timeEvents[i] = new TimeEvent {
                     eventName = evt.eventName,
                     triggerTime = evt.triggerTime,
                     hasTriggered = true, // Mark as triggered // 标记为已触发
                     eventData = evt.eventData,
                     eventType = evt.eventType
                };
            }
        }
    }

    // --- MODIFIED: ResetTimeEvents now clears the list ---
    // --- 已修改：ResetTimeEvents 现在清除列表 ---
    /// <summary>
    /// Resets the elapsed timer and clears all registered timed events.
    /// 重置经过的计时器并清除所有已注册的定时事件。
    /// Typically called when a game session starts or ends.
    /// 通常在游戏会话开始或结束时调用。
    /// </summary>
    public void ResetTimeEvents()
    {
        elapsedTime = 0f;
        int count = timeEvents.Count;
        timeEvents.Clear(); // Remove all registered timed events // 移除所有已注册的定时事件
        Debug.Log($"[EventManager] Timer reset and {count} timed events cleared.");
    }
    // --- END MODIFICATION ---


    // 获取当前时间（秒）(Get current elapsed time in seconds)
    public float GetSeconds() // Return float for more precision // 返回 float 以获得更高精度
    {
        return elapsedTime;
    }

    // 删除事件，防止内存泄露 (Remove listeners to prevent memory leaks)
    public void RemoveAllListenersForType(Type eventType)
    {
        if (eventListeners.Remove(eventType)) // Remove directly returns bool // 直接移除返回 bool
        {
            Debug.Log($"[EventManager] Removed all listeners for event type: {eventType.Name}");
        }
    }

    // 你甚至可以提供泛型版本 (You can even provide a generic version)
    public void RemoveAllListenersForType<T>() where T : class
    {
        RemoveAllListenersForType(typeof(T));
    }


    // Optional: Clean up specific listeners on destroy if needed
    // 可选：如果需要，在销毁时清理特定的监听器
    void OnDestroy()
    {
        ResetTimeEvents();
    }
}
