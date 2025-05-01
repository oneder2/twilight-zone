using UnityEngine;
// 确保你引用了包含事件定义的命名空间（如果需要）
// using YourEventsNamespace;

/// <summary>
/// 这个脚本附加到希望响应 Timeline 信号的目标 GameObject 上（位于另一个场景）。
/// This script is attached to the target GameObject (in another scene)
/// that should react to the Timeline signal.
/// 它通过监听 EventManager 中的全局事件来实现响应。
/// It listens for global events from the EventManager to react.
/// </summary>
public class TargetScript : MonoBehaviour
{
    [Header("配置 (Configuration)")]
    [Tooltip("如果此脚本控制特定对象（如门），请在此处设置其唯一ID以匹配事件数据。")]
    [SerializeField] private string specificTargetID = "SecretPassageDoor"; // 示例ID (Example ID)

    [Tooltip("（可选）希望在此脚本响应时播放的动画状态或触发器名称。")]
    [SerializeField] private string animationTriggerName = "Open"; // 示例动画触发器 (Example animation trigger)

    private Animator animator; // （可选）缓存 Animator 组件 (Optional Animator cache)

    void Awake()
    {
        // （可选）获取 Animator 组件
        // (Optional) Get the Animator component
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        Debug.Log($"[TargetScript] Enabling on {gameObject.name} in scene '{gameObject.scene.name}'. Subscribing to events.");
        // 确保 EventManager 存在
        // Ensure EventManager exists
        if (EventManager.Instance != null)
        {
            // 注册监听你感兴趣的全局事件
            // Register listeners for the global events you are interested in
            EventManager.Instance.AddListener<OpenSpecificDoorEvent>(HandleOpenDoorEvent);
            EventManager.Instance.AddListener<MyTimelineActionEvent>(HandleSimpleActionEvent);
        }
        else
        {
             Debug.LogError($"[TargetScript] EventManager.Instance is null on Enable for {gameObject.name}! Cannot subscribe.");
        }
    }

    void OnDisable()
    {
         Debug.Log($"[TargetScript] Disabling on {gameObject.name} in scene '{gameObject.scene.name}'. Unsubscribing from events.");
        // 非常重要：取消注册监听器以防止错误
        // Very important: Unregister listeners to prevent errors
        // 检查 EventManager 是否仍然存在
        // Check if EventManager still exists
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<OpenSpecificDoorEvent>(HandleOpenDoorEvent);
            EventManager.Instance.RemoveListener<MyTimelineActionEvent>(HandleSimpleActionEvent);
        }
    }

    /// <summary>
    /// 处理 OpenSpecificDoorEvent 事件的方法。
    /// Method to handle the OpenSpecificDoorEvent.
    /// </summary>
    private void HandleOpenDoorEvent(OpenSpecificDoorEvent eventData)
    {
        // 检查事件传递的ID是否与此脚本的目标ID匹配
        // Check if the ID passed by the event matches this script's target ID
        if (eventData != null && eventData.DoorID == specificTargetID)
        {
            Debug.Log($"[TargetScript] Received OpenSpecificDoorEvent for MY target ID: {eventData.DoorID} on {gameObject.name}. Performing action!");

            // 在这里执行响应动作
            // Perform the reaction action here
            PerformTargetAction();
        }
        // else // 可选：如果ID不匹配，可以记录日志或忽略
        // {
        //     Debug.Log($"[TargetScript] Received OpenSpecificDoorEvent for ID: {eventData?.DoorID ?? "NULL"}, but it doesn't match my target ID ({specificTargetID}). Ignoring.");
        // }
    }

    /// <summary>
    /// 处理 MyTimelineActionEvent 事件的方法。
    /// Method to handle the MyTimelineActionEvent.
    /// </summary>
    private void HandleSimpleActionEvent(MyTimelineActionEvent eventData)
    {
        Debug.Log($"[TargetScript] Received MyTimelineActionEvent on {gameObject.name}. Performing action!");

        // 在这里执行响应动作
        // Perform the reaction action here
        PerformTargetAction();
    }

    /// <summary>
    /// 封装了目标 GameObject 需要执行的具体响应动作。
    /// Encapsulates the specific action the target GameObject should perform.
    /// </summary>
    private void PerformTargetAction()
    {
        // 在这里实现你的具体逻辑，例如：
        // Implement your specific logic here, for example:

        // 1. 播放动画
        // 1. Play an animation
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            animator.SetTrigger(animationTriggerName);
            Debug.Log($"[TargetScript] Triggered animation '{animationTriggerName}' on {gameObject.name}.");
        }

        // 2. 启用/禁用 GameObject
        // 2. Enable/Disable the GameObject
        // gameObject.SetActive(true); // or false

        // 3. 改变某个组件的属性
        // 3. Change a property of some component
        // GetComponent<SpriteRenderer>().color = Color.red;

        // 4. 调用其他方法
        // 4. Call other methods
        // GetComponent<SomeOtherScript>()?.DoSomething();

        // ...等等 (etc.)
    }
}
