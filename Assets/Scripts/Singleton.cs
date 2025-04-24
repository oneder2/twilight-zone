// File: Scripts/Singleton.cs
using UnityEngine;

/// <summary>
/// A generic base class for creating Singleton components.
/// 用于创建单例组件的泛型基类。
/// Ensures only one instance exists and optionally persists across scenes.
/// 确保只有一个实例存在，并可选择跨场景持久存在。
/// </summary>
/// <typeparam name="T">The type of the Singleton component. / 单例组件的类型。</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // The static instance of the Singleton.
    // 单例的静态实例。
    private static T _instance;

    // Lock object for thread safety during instance access (optional but good practice).
    // 用于在实例访问期间保证线程安全的锁对象（可选但建议实践）。
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets the singleton instance. If it doesn't exist, it tries to find it or creates it.
    /// 获取单例实例。如果不存在，则尝试查找或创建它。
    /// </summary>
    public static T Instance
    {
        get
        {
            // Lock for thread safety, preventing race conditions if accessed from multiple threads.
            // 为线程安全加锁，防止从多个线程访问时出现竞态条件。
            lock (_lock)
            {
                // If the instance is already found, return it.
                // 如果实例已被找到，则返回它。
                if (_instance != null)
                {
                    // --- LOGGING ADDED ---
                    // --- 已添加日志 ---
                    // Debug.Log($"[{typeof(T).Name} Singleton Instance Getter] Returning existing instance (ID: {_instance.GetInstanceID()})."); // Can be very verbose // 可能非常冗余
                    return _instance;
                }

                // Try to find an existing instance in the scene(s).
                // 尝试在场景中查找现有实例。
                // FindObjectsOfType is slow, FindFirstObjectByType is generally preferred in modern Unity.
                // FindObjectsOfType 较慢，在现代 Unity 中通常首选 FindFirstObjectByType。
                _instance = FindFirstObjectByType<T>();

                // If an instance was found, log it and return.
                // 如果找到了实例，记录并返回。
                if (_instance != null)
                {
                     // --- LOGGING ADDED ---
                     // --- 已添加日志 ---
                     Debug.Log($"[{typeof(T).Name} Singleton Instance Getter] Found existing instance in scene (ID: {_instance.GetInstanceID()}).");
                    return _instance;
                }

                // If no instance exists, create a new GameObject and add the component.
                // 如果不存在实例，则创建一个新的 GameObject 并添加该组件。
                Debug.LogWarning($"[{typeof(T).Name} Singleton Instance Getter] No instance found. Creating a new one. This might indicate it wasn't present in the initial scene or was destroyed.");
                GameObject singletonObject = new GameObject(typeof(T).Name + " (Singleton)");
                _instance = singletonObject.AddComponent<T>();
                // Note: The Awake method of the newly added component will handle DontDestroyOnLoad.
                // 注意：新添加组件的 Awake 方法将处理 DontDestroyOnLoad。
                return _instance;
            }
        }
    }

    /// <summary>
    /// Called when the script instance is being loaded. Handles instance uniqueness and persistence.
    /// 在加载脚本实例时调用。处理实例唯一性和持久性。
    /// </summary>
    protected virtual void Awake()
    {
        // Lock for thread safety during Awake modifications.
        // 在 Awake 修改期间为线程安全加锁。
        lock (_lock)
        {
            if (_instance == null)
            {
                // This is the first instance. Assign it and make it persistent.
                // 这是第一个实例。分配它并使其持久化。
                _instance = this as T;
                // DontDestroyOnLoad(gameObject); // Apply DontDestroyOnLoad to the FIRST instance // 对第一个实例应用 DontDestroyOnLoad
                Debug.Log($"[{typeof(T).Name} Singleton Awake] First instance assigned (ID: {GetInstanceID()}) and marked DontDestroyOnLoad in scene '{gameObject.scene.name}'.");
            }
            else if (_instance != this)
            {
                // A duplicate instance exists. Destroy this new one.
                // 存在重复实例。销毁这个新的实例。
                Debug.LogWarning($"[{typeof(T).Name} Singleton Awake] Duplicate instance detected (New GO: '{gameObject.name}', ID: {GetInstanceID()}, Scene: '{gameObject.scene.name}'). Existing instance ID: {_instance.GetInstanceID()}. Destroying self.");
                Destroy(gameObject); // Destroy the duplicate // 销毁重复项
            }
            // If _instance == this, Awake is called on the already assigned persistent instance. Do nothing.
            // 如果 _instance == this，则 Awake 在已分配的持久实例上被调用。无需执行任何操作。
        }
    }

    // Optional: Add OnDestroy logic to clear the static instance if the singleton object is somehow destroyed.
    // 可选：如果单例对象以某种方式被销毁，添加 OnDestroy 逻辑以清除静态实例。
    // protected virtual void OnDestroy()
    // {
    //     lock (_lock)
    //     {
    //         if (_instance == this)
    //         {
    //             _instance = null;
    //             Debug.Log($"[{typeof(T).Name} Singleton OnDestroy] Instance (ID: {GetInstanceID()}) destroyed. Static reference cleared.");
    //         }
    //     }
    // }
}
