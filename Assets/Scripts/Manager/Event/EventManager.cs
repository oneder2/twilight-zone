using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : Singleton<EventManager>
{
    // 存储事件类型和对应监听器的字典
    private Dictionary<Type, Action<object>> eventListeners = new Dictionary<Type, Action<object>>();

    // 时间事件的结构体
    [System.Serializable]
    private struct TimeEvent
    {
        public string eventName;       // 事件名称（用于调试或标识）
        public int triggerTime;        // 触发时间（秒）
        public bool hasTriggered;      // 是否已触发
        public object eventData;       // 触发时的事件数据
        public Type eventType;         // 事件类型（泛型T的类型）
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
            triggerTime = triggerTime,
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
                // 触发对应类型的事件
                if (eventListeners.ContainsKey(evt.eventType))
                {
                    eventListeners[evt.eventType]?.Invoke(evt.eventData);
                    Debug.Log($"时间 {evt.triggerTime} 秒，触发事件：{evt.eventName}");
                }

                // 标记为已触发
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
}