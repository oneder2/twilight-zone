using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    // 单例模式
    private static EventManager instance;
    public static EventManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EventManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("EventManager");
                    instance = obj.AddComponent<EventManager>();
                }
            }
            return instance;
        }
    }

    // 存储事件类型和对应监听器的字典
    private Dictionary<Type, Action<object>> eventListeners = new Dictionary<Type, Action<object>>();

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

    // 触发事件
    public void TriggerEvent<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType]?.Invoke(eventData);
        }
    }
}