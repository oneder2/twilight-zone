// File: Scripts/DeactivateOnStatus.cs (Revised)
using UnityEngine;

/// <summary>
/// Destroys the GameObject this script is attached to when the game
/// transitions into a specific target GameStatus.
/// 当游戏转换到特定的目标 GameStatus 时，销毁附加此脚本的 GameObject。
/// Listens for the GameStatusChangedEvent broadcast by the EventManager.
/// 监听由 EventManager 广播的 GameStatusChangedEvent。
/// </summary>
public class DeactivateOnStatus : MonoBehaviour // Renamed from DestroyOnStatus for clarity // 为清晰起见从 DestroyOnStatus 重命名
{
    [Tooltip("The GameStatus that will trigger the destruction of this GameObject.")]
    // [Tooltip("将触发此 GameObject 销毁的 GameStatus。")]
    [SerializeField] private GameStatus deactivateOnStatus = GameStatus.GameOver; // Example default, set in Inspector // 示例默认值，在 Inspector 中设置

    private bool isListenerRegistered = false; // Track registration status // 跟踪注册状态
    private bool isDestroying = false; // Prevent multiple destroy attempts // 防止多次销毁尝试

    void OnEnable()
    {
        // Subscribe only if not already registered and not already destroying
        // 仅在尚未注册且尚未销毁时订阅
        if (!isListenerRegistered && !isDestroying && EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            isListenerRegistered = true;
            // Debug.Log($"{gameObject.name}: Subscribed to GameStatusChangedEvent for deactivation on {deactivateOnStatus}."); // Less verbose // 减少冗余
        }
        else if (EventManager.Instance == null)
        {
            Debug.LogError($"[{gameObject.name} DeactivateOnStatus] EventManager.Instance is null on Enable. Cannot subscribe.");
        }
    }

    void OnDisable()
    {
        // Unsubscribe only if registered
        // 仅在已注册时取消订阅
        // Check EventManager.Instance existence because it might be destroyed before this OnDisable runs during application quit
        // 检查 EventManager.Instance 是否存在，因为它可能在应用程序退出期间此 OnDisable 运行之前被销毁
        if (isListenerRegistered && EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
            // Debug.Log($"{gameObject.name}: Unsubscribed from GameStatusChangedEvent."); // Less verbose // 减少冗余
        }
        // Reset flag regardless of whether unsubscribe happened (in case EventManager was already gone)
        // 无论取消订阅是否发生，都重置标志（以防 EventManager 已消失）
        isListenerRegistered = false;
    }

    /// <summary>
    /// Handles the GameStatusChangedEvent. Checks if the new status matches the target status.
    /// 处理 GameStatusChangedEvent。检查新状态是否与目标状态匹配。
    /// </summary>
    /// <param name="eventData">The event data containing the new and previous status. / 包含新旧状态的事件数据。</param>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        // Prevent action if already destroying or if the component/GO is no longer valid
        // 如果已在销毁或组件/GO 不再有效，则阻止操作
        if (isDestroying || this == null || !this.enabled) return;

        // Check if the new game status is the one we want to deactivate on
        // 检查新游戏状态是否是我们想要停用的状态
        if (eventData.NewStatus == deactivateOnStatus)
        {
            Debug.Log($"[DeactivateOnStatus] Deactivating self ({gameObject.name}) due to game status change to {deactivateOnStatus}.");
            isDestroying = true; // Mark as destroying to prevent re-entry // 标记为正在销毁以防止重入

            // --- Revised Destruction ---
            // --- 修订后的销毁 ---
            // Unsubscribe immediately *before* destroying.
            // 在销毁*之前*立即取消订阅。
            // Check EventManager.Instance existence again, as it might be destroyed between the event trigger and this handler execution.
            // 再次检查 EventManager.Instance 是否存在，因为它可能在事件触发和此处理程序执行之间被销毁。
            if (isListenerRegistered && EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
                isListenerRegistered = false; // Update flag // 更新标志
                 Debug.Log($"[DeactivateOnStatus] Unsubscribed listener immediately before destroy on {gameObject.name}.");
            }

            // Destroy the GameObject this script is attached to
            // 销毁附加此脚本的 GameObject
            Destroy(gameObject);
            // OnDisable might be called shortly after Destroy starts the process,
            // but we've already manually unregistered the listener for safety.
            // 在 Destroy 开始该过程后不久可能会调用 OnDisable，
            // 但为了安全起见，我们已经手动注销了监听器。
        }
    }
}
