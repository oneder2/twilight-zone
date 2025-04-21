using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : Singleton<EventManager>
{
    // The dictionary which store event type and listener
    private Dictionary<Type, Action<object>> eventListeners = new Dictionary<Type, Action<object>>();

    // Structor of Time event
    [System.Serializable]
    private struct TimeEvent
    {
        public string eventName;       // Event Name(To debug or marking)
        public int triggerTime;        // Trigger time(second)
        public bool hasTriggered;      // if has been triggered
        public object eventData;       // Event data when triggered
        public Type eventType;         // Event Type
    }

    private float elapsedTime = 0f; // 累计时间（秒）
    private List<TimeEvent> timeEvents = new List<TimeEvent>(); // 存储所有时间事件

    void Update()
    {
        elapsedTime += Time.deltaTime; // 累加时间
        CheckTimeEvents(); // 检查时间事件
    }


    // 注册监听器
    public void AddListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (!eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = null;
        }
        eventListeners[eventType] += (obj) => listener(obj as T);
    }

    // 移除监听器
    public void RemoveListener<T>(Action<T> listener) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] -= (obj) => listener(obj as T);
        }
    }

    // 触发即时事件
    public void TriggerEvent<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType]?.Invoke(eventData);
        }
    }

    // 注册定时事件
    public void RegisterTimeEvent<T>(string eventName, int triggerTime, T eventData) where T : class
    {
        TimeEvent newEvent = new TimeEvent
        {
            eventName = eventName,
            triggerTime = GetSeconds() + triggerTime,
            hasTriggered = false,
            eventData = eventData,
            eventType = typeof(T)
        };
        timeEvents.Add(newEvent);
        Debug.Log($"注册定时事件：{eventName} 在 {triggerTime} 秒触发");
    }

    // 检查并触发时间事件
    private void CheckTimeEvents()
    {
        int currentSeconds = Mathf.FloorToInt(elapsedTime);
        for (int i = 0; i < timeEvents.Count; i++)
        {
            TimeEvent evt = timeEvents[i];
            if (!evt.hasTriggered && currentSeconds >= evt.triggerTime)
            {
                if (eventListeners.ContainsKey(evt.eventType))
                {
                    eventListeners[evt.eventType]?.Invoke(evt.eventData);
                    Debug.Log($"时间 {evt.triggerTime} 秒，触发事件：{evt.eventName}");
                }
                evt.hasTriggered = true;
                timeEvents[i] = evt;
            }
        }
    }

    // 重置计时和所有时间事件状态
    public void ResetTimeEvents()
    {
        elapsedTime = 0f;
        for (int i = 0; i < timeEvents.Count; i++)
        {
            TimeEvent evt = timeEvents[i];
            evt.hasTriggered = false;
            timeEvents[i] = evt;
        }
    }

    // 获取当前时间（秒）
    public int GetSeconds()
    {
        return Mathf.FloorToInt(elapsedTime);
    }

    // 删除事件，防止内存泄露
    public void RemoveAllListenersForType(Type eventType)
    {
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = null; // 清空委托链
            // 或者 eventListeners.Remove(eventType); // 直接移除该类型的条目
            Debug.Log($"Removed all listeners for event type: {eventType.Name}");
        }
    }

    // 你甚至可以提供泛型版本
    public void RemoveAllListenersForType<T>() where T : class
    {
        RemoveAllListenersForType(typeof(T));
    }


    void OnDestroy()
    {
        RemoveAllListenersForType<StageChangeEvent>();
    }
}

