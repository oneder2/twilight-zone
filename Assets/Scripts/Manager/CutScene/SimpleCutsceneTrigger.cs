using UnityEngine;
using UnityEngine.Playables; // Required for PlayableAsset and PlayableDirector // PlayableAsset 和 PlayableDirector 所需

/// <summary>
/// Triggers a specific Timeline Asset to play on a specified PlayableDirector
/// when an object with the correct layer enters the trigger collider.
/// 当具有正确图层的对象进入触发器碰撞体时，
/// 在指定的 PlayableDirector 上触发播放特定的 Timeline Asset。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SimpleCutsceneTrigger : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("The Timeline Asset to play when triggered.")]
    [SerializeField] private PlayableAsset timelineToPlay; // Assign the specific Timeline Asset here // 在此处分配特定的 Timeline Asset

    [Tooltip("The PlayableDirector component that will play the timeline. If null, attempts to find one on this GameObject.")]
    [SerializeField] private PlayableDirector targetDirector; // Assign the Director (e.g., a central one or one on this object) // 分配 Director（例如，中央 Director 或此对象上的 Director）

    [Header("Trigger Settings")]
    [Tooltip("Should this trigger only activate once?")]
    [SerializeField] private bool triggerOnce = true; // 此触发器是否只激活一次？
    [Tooltip("Layer mask to specify what layers can trigger this (e.g., Player layer).")]
    [SerializeField] private LayerMask triggeringLayer; // 指定哪些图层可以触发此触发器（例如，Player 图层）

    // --- Removed coroutine name field ---
    // [SerializeField] private string cutsceneCoroutineName; // REMOVED // 已移除

    private bool hasTriggered = false;

    void Awake()
    {
        // Validate essential assignments // 验证基本分配
        if (timelineToPlay == null)
        {
             Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Timeline Asset (PlayableAsset) to be assigned!", gameObject);
             enabled = false;
        }
        if (targetDirector == null)
        {
            // Attempt to find PlayableDirector on the same GameObject if not assigned
            // 如果未分配，则尝试在同一 GameObject 上查找 PlayableDirector
            targetDirector = GetComponent<PlayableDirector>();
            if (targetDirector == null)
            {
                 Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a PlayableDirector assigned or present on the same GameObject!", gameObject);
                 enabled = false;
            }
        }

        // Ensure collider is a trigger // 确保碰撞体是触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        else { Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Collider2D.", gameObject); enabled = false; }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check trigger conditions (once, layer, game state) // 检查触发条件（一次、图层、游戏状态）
        if (triggerOnce && hasTriggered) return;
        if (triggeringLayer.value != 0 && ((1 << other.gameObject.layer) & triggeringLayer) == 0) return;

        // Check GameRunManager state - Don't trigger if already in a cutscene or loading etc.
        // 检查 GameRunManager 状态 - 如果已处于过场动画或加载等状态，则不触发
        if (GameRunManager.Instance == null)
        {
             Debug.LogError("GameRunManager instance not found! Cannot trigger timeline.", gameObject);
             return;
        }
        // Prevent triggering if game is not in a playable state or already in a cutscene
        // 如果游戏不处于可玩状态或已处于过场动画中，则阻止触发
        if (GameRunManager.Instance.CurrentStatus != GameStatus.Playing)
        {
             Debug.LogWarning($"Cutscene trigger on {gameObject.name} activated, but Game Status is '{GameRunManager.Instance.CurrentStatus}'. Ignoring.");
             return;
        }

        // 2. Play the Timeline // 播放 Timeline
        if (targetDirector != null && timelineToPlay != null)
        {
            Debug.Log($"{gameObject.name} triggered by {other.name}. Playing Timeline: '{timelineToPlay.name}' on Director '{targetDirector.name}'.");

            // Assign the Timeline Asset to the director
            // 将 Timeline Asset 分配给 director
            targetDirector.playableAsset = timelineToPlay;

            // --- IMPORTANT: Handle potential conflicts --- // --- 重要：处理潜在冲突 ---
            // If this director might already be playing something else, decide how to handle it.
            // Options: Stop the current playback, queue, or ignore the new request.
            // 如果此 director 可能已经在播放其他内容，请决定如何处理。
            // 选项：停止当前播放、排队或忽略新请求。
            if (targetDirector.state == PlayState.Playing)
            {
                 Debug.LogWarning($"Target Director '{targetDirector.name}' is already playing. Stopping previous playback to play '{timelineToPlay.name}'.");
                 targetDirector.Stop(); // Stop previous timeline before starting new one // 在开始新的时间轴之前停止上一个时间轴
            }

            // Play the timeline // 播放时间轴
            targetDirector.Play();

            // The Timeline itself should have signals to call GameRunManager.EnterCutsceneState()
            // Timeline 本身应具有调用 GameRunManager.EnterCutsceneState() 的信号

            // 3. Mark as triggered if set to trigger once // 如果设置为触发一次，则标记为已触发
            if (triggerOnce)
            {
                hasTriggered = true;
                // Optional: Disable the trigger GameObject after use // 可选：使用后禁用触发器 GameObject
                // gameObject.SetActive(false);
            }
        }
        else
        {
            // This should ideally be caught in Awake, but added as a safety check
            // 理想情况下，这应该在 Awake 中捕获，但作为安全检查添加
            Debug.LogError($"Cannot play timeline from {gameObject.name}. Target Director or Timeline Asset is missing.", gameObject);
        }
    }
}
