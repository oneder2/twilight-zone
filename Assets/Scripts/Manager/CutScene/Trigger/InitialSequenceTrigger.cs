// File: Scripts/Manager/CutScene/init/InitialSequenceTrigger.cs (事件驱动版本)
using UnityEngine;
using UnityEngine.Playables;
// using YourEventsNamespace; // 如果事件在命名空间中

/// <summary>
/// Listens for the GameReadyToPlayEvent and then triggers an initial Timeline sequence
/// based on game progress.
/// 监听 GameReadyToPlayEvent 事件，然后在事件触发后根据游戏进度触发初始 Timeline 序列。
/// Destroys itself after triggering or if conditions aren't met.
/// 在触发后或条件不满足时销毁自身。
/// </summary>
public class InitialSequenceTrigger : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("Assign the PlayableDirector for the DEFAULT opening sequence.")]
    [SerializeField] private PlayableDirector defaultOpeningDirector;

    [Tooltip("Assign the PlayableDirector for the sequence after completing one loop (Optional).")]
    [SerializeField] private PlayableDirector loop1OpeningDirector;
    // Add more directors for other progress states if needed

    private bool hasTriggered = false; // 防止重复触发 (Prevent multiple triggers)

    void OnEnable()
    {
        // 注册监听器
        // Register listener
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameReadyToPlayEvent>(HandleGameReadyToPlay);
            Debug.Log($"[InitialSequenceTrigger] Enabled on {gameObject.name}. Subscribed to GameReadyToPlayEvent.");
        }
        else
        {
            Debug.LogError($"[InitialSequenceTrigger] EventManager not found on Enable for {gameObject.name}! Cannot subscribe.");
        }
    }

    void OnDisable()
    {
        // 取消注册监听器
        // Unregister listener
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameReadyToPlayEvent>(HandleGameReadyToPlay);
            Debug.Log($"[InitialSequenceTrigger] Disabled on {gameObject.name}. Unsubscribed from GameReadyToPlayEvent.");
        }
    }

    /// <summary>
    /// Handles the GameReadyToPlayEvent. Selects and plays the appropriate Timeline.
    /// 处理 GameReadyToPlayEvent 事件。选择并播放合适的 Timeline。
    /// </summary>
    private void HandleGameReadyToPlay(GameReadyToPlayEvent eventData)
    {
        // 防止重复触发
        // Prevent multiple triggers
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log("[InitialSequenceTrigger] Received GameReadyToPlayEvent. Proceeding with Timeline selection.");

        // --- 检查首次加载逻辑 (Check first load logic) ---
        // (这部分逻辑仍然相关，但现在由事件触发)
        // (This logic is still relevant, but now triggered by the event)
        if (GameRunManager.IsInitialLoadComplete) // 此时 IsInitialLoadComplete 应该已经被 StartGameManager 设置为 true
        {
             Debug.Log("[InitialSequenceTrigger] GameReadyToPlay event received (Not the initial load).");
        }
        else
        {
             Debug.Log("[InitialSequenceTrigger] GameReadyToPlay event received (Initial load).");
        }

        // --- 选择要播放的 Timeline (Select Timeline to play) ---
        PlayableDirector directorToPlay = null;
        int loops = 0;

        if (ProgressManager.Instance != null)
        {
            loops = ProgressManager.Instance.LoopsCompleted;
            Debug.Log($"[InitialSequenceTrigger] Current loops completed: {loops}");

            if (loops >= 1 && loop1OpeningDirector != null)
            {
                directorToPlay = loop1OpeningDirector;
                Debug.Log("[InitialSequenceTrigger] Selecting Loop 1 opening sequence.");
            }
            else if (defaultOpeningDirector != null)
            {
                 directorToPlay = defaultOpeningDirector;
                 Debug.Log("[InitialSequenceTrigger] Selecting DEFAULT opening sequence.");
            }
        }
        else
        {
            Debug.LogWarning("[InitialSequenceTrigger] ProgressManager not found. Falling back to default opening sequence.");
            directorToPlay = defaultOpeningDirector;
        }

        // --- 播放选定的 Timeline (Play the selected Timeline) ---
        if (directorToPlay != null)
        {
            // 状态保证是 Playing，因为事件是在之后触发的
            // Status is guaranteed to be Playing as the event is triggered after setting it
            Debug.Log($"[InitialSequenceTrigger] Playing Timeline: {directorToPlay.playableAsset?.name ?? "Unnamed"}");
            directorToPlay.Play();
            // Timeline 内部的信号会处理状态切换到 InCutscene
            // Signals within the Timeline will handle changing state to InCutscene
        }
        else
        {
            Debug.Log("[InitialSequenceTrigger] No suitable opening sequence director found to play.");
        }

        // --- 触发后销毁自身 (Destroy self after triggering) ---
        // 可以稍微延迟销毁，确保 Timeline 确实开始播放了
        // Can delay destruction slightly to ensure Timeline actually started playing
        Debug.Log("[InitialSequenceTrigger] Trigger sequence complete. Destroying self shortly.");
        // Destroy(gameObject); // 立即销毁 (Destroy immediately)
         StartCoroutine(DestroyAfterDelay(0.1f)); // 延迟销毁 (Destroy after a short delay)
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null) // 检查脚本是否还存在 (Check if script still exists)
        {
             Destroy(gameObject);
        }
    }
}
