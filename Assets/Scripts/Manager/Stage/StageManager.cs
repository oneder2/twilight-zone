// File: Scripts/Manager/GameStage/StageManager.cs
using UnityEngine;

/// <summary>
/// Manages the game's progression through different stages, applying settings like lighting, music, and enemy spawning.
/// 管理游戏经历不同阶段的进程，应用光照、音乐和敌人生成等设置。
/// Assumes Singleton pattern.
/// 假设使用单例模式。
/// </summary>
public class StageManager : Singleton<StageManager> // Inherit from your Singleton base class // 继承自你的 Singleton 基类
{
    [Tooltip("Assign all StageData ScriptableObjects here in order.(请在此处按顺序分配所有 StageData ScriptableObject。)")]
    [SerializeField] private StageData[] stages; // 在Inspector中配置所有阶段数据 (Configure all stage data in the Inspector)

    private int currentStageId = 0; // 当前阶段ID (Current stage ID)

    // --- Unity Methods ---
    // --- Unity 方法 ---

    void Start()
    {
        // --- Example Time Event Registration ---
        // --- 示例时间事件注册 ---
        // You'll likely replace these hardcoded times with logic based on game events (e.g., boss defeat)
        // 你很可能会用基于游戏事件（例如，Boss 被击败）的逻辑替换这些硬编码时间
        if (EventManager.Instance != null)
        {
            // Example: Trigger stage changes based on time (can be replaced by other triggers)
            // 示例：基于时间触发阶段变化（可以被其他触发器替换）
            EventManager.Instance.RegisterTimeEvent("切换到阶段1 (Switch to Stage 1)", 60, new StageChangeEvent(1));
            EventManager.Instance.RegisterTimeEvent("切换到阶段2 (Switch to Stage 2)", 120, new StageChangeEvent(2));
            EventManager.Instance.RegisterTimeEvent("切换到阶段3 (Switch to Stage 3)", 180, new StageChangeEvent(3));
            EventManager.Instance.RegisterTimeEvent("切换到阶段4 (Switch to Stage 4)", 240, new StageChangeEvent(4));
            EventManager.Instance.RegisterTimeEvent("切换到阶段5 (Switch to Stage 5)", 300, new StageChangeEvent(5));
            // EventManager.Instance.RegisterTimeEvent("切换到最终阶段 (Switch to Final Stage)", 360, new StageChangeEvent(6)); // Example final stage // 示例最终阶段

            // Listen for stage change events triggered elsewhere (e.g., by time, boss defeat)
            // 监听在别处触发的阶段变化事件（例如，通过时间、Boss 被击败）
            EventManager.Instance.AddListener<StageChangeEvent>(OnStageChanged);
            Debug.Log("[StageManager] Subscribed to StageChangeEvent.");
        }
        else
        {
             Debug.LogError("[StageManager] EventManager instance not found! Cannot register time events or listen for stage changes.");
        }


        // Initialize the first stage settings immediately on start
        // 在启动时立即初始化第一阶段的设置
        // Make sure stages array is populated
        // 确保 stages 数组已填充
        if (stages != null && stages.Length > 0)
        {
             // Set initial stage ID safely
             // 安全地设置初始阶段 ID
             currentStageId = 0; // Start at stage 0
             ApplyStageSettings();
             Debug.Log($"[StageManager] Initialized with Stage: {stages[currentStageId].stageName}");
        } else {
             Debug.LogError("[StageManager] Stages array is not assigned or empty in the Inspector!", this);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when destroyed to prevent memory leaks
        // 在销毁时取消订阅事件以防止内存泄漏
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<StageChangeEvent>(OnStageChanged);
             Debug.Log("[StageManager] Unsubscribed from StageChangeEvent.");
        }
    }

    // --- Event Handlers ---
    // --- 事件处理器 ---

    /// <summary>
    /// Handles the StageChangeEvent, updating the current stage and applying its settings.
    /// 处理 StageChangeEvent，更新当前阶段并应用其设置。
    /// </summary>
    /// <param name="stageEvent">The event data containing the target stage ID. / 包含目标阶段 ID 的事件数据。</param>
    private void OnStageChanged(StageChangeEvent stageEvent)
    {
        if (stageEvent == null) return;
        SetStage(stageEvent.StageId);
    }

    // --- Stage Management Logic ---
    // --- 阶段管理逻辑 ---

    /// <summary>
    /// Sets the current stage ID and applies the corresponding settings.
    /// 设置当前阶段 ID 并应用相应的设置。
    /// </summary>
    /// <param name="stageId">The ID of the stage to switch to. / 要切换到的阶段的 ID。</param>
    public void SetStage(int stageId)
    {
        // Validate the stage ID
        // 验证阶段 ID
        if (stages == null || stageId < 0 || stageId >= stages.Length)
        {
            Debug.LogError($"[StageManager] Invalid stage ID requested: {stageId}. Max index is {stages?.Length - 1 ?? -1}.", this);
            return;
        }

        // Check if already in the requested stage
        // 检查是否已处于请求的阶段
        if (currentStageId == stageId)
        {
             Debug.Log($"[StageManager] Already in stage {stageId} ({stages[stageId]?.stageName ?? "N/A"}). No change needed.");
             return; // Avoid reapplying settings unnecessarily // 避免不必要地重新应用设置
        }


        currentStageId = stageId;
        Debug.Log($"[StageManager] Switching to Stage ID: {currentStageId} ({stages[currentStageId]?.stageName ?? "Error: Stage data missing!"})");
        ApplyStageSettings();
    }

    /// <summary>
    /// Applies all settings defined in the current stage's StageData.
    /// 应用当前阶段 StageData 中定义的所有设置。
    /// </summary>
    private void ApplyStageSettings()
    {
        // Ensure currentStageId is valid before accessing stages array
        // 在访问 stages 数组之前，确保 currentStageId 有效
        if (stages == null || currentStageId < 0 || currentStageId >= stages.Length || stages[currentStageId] == null)
        {
            Debug.LogError($"[StageManager] Cannot apply settings. Invalid stage ID ({currentStageId}) or missing StageData.", this);
            return;
        }

        StageData currentStage = stages[currentStageId];
        Debug.Log($"[StageManager] Applying settings for Stage: {currentStage.stageName}");

        // 1. Update Lighting (using LightManager Singleton)
        // 1. 更新光照（使用 LightManager 单例）
        if (LightManager.Instance != null)
        {
            LightManager.Instance.UpdateLighting(currentStage.lightIntensity, currentStage.lightColor);
        }
        else { Debug.LogWarning("[StageManager] LightManager instance not found. Cannot update lighting."); }

        // 2. Update Background Music (using AudioManager Singleton)
        // 2. 更新背景音乐（使用 AudioManager 单例）
        if (AudioManager.Instance != null)
        {
            // Play the music defined for this stage (handles None case internally)
            // 播放为此阶段定义的音乐（内部处理 None 情况）
            AudioManager.Instance.PlayMusic(currentStage.trackId);
        }
        else { Debug.LogWarning("[StageManager] AudioManager instance not found. Cannot update music."); }


        // --- NEW: Configure Enemy Spawner ---
        // --- 新增：配置敌人生成器 ---
        if (EnemySpawner.Instance != null)
        {
             // Pass the entire StageData object to the spawner
             // 将整个 StageData 对象传递给生成器
             EnemySpawner.Instance.ConfigureSpawner(currentStage);
        }
        else
        {
             // Log a warning if the spawner isn't ready yet. This might happen
             // if StageManager's Start runs before EnemySpawner's Awake.
             // 如果生成器尚未准备好，则记录警告。如果 StageManager 的 Start 在 EnemySpawner 的 Awake 之前运行，则可能会发生这种情况。
             Debug.LogWarning("[StageManager] EnemySpawner instance not found. Cannot configure enemy spawning for this stage. It might initialize later.");
        }
        // --- End Enemy Spawner Configuration ---


        // 3. Update other stage-specific elements (e.g., display dialogue, change background visuals)
        // 3. 更新其他特定于阶段的元素（例如，显示对话、更改背景视觉效果）
        if (!string.IsNullOrEmpty(currentStage.dialogueMessage) && DialogueManager.Instance != null)
        {
            // Example: Show a brief stage notification dialogue
            // 示例：显示简短的阶段通知对话
            // DialogueManager.Instance.ShowDialogue(currentStage.dialogueMessage); // Use ShowDialogue(string) for temporary messages
                                                                              // 对临时消息使用 ShowDialogue(string)
        }
        // Add logic here to change background sprites, activate/deactivate stage-specific objects, etc.
        // 在此处添加逻辑以更改背景精灵图、激活/停用特定于阶段的对象等。

        Debug.Log($"[StageManager] Finished applying settings for Stage: {currentStage.stageName}");
    }

    /// <summary>
    /// Gets the data for the currently active stage.
    /// 获取当前活动阶段的数据。
    /// </summary>
    /// <returns>The current StageData, or null if invalid. / 当前的 StageData，如果无效则为 null。</returns>
    public StageData GetCurrentStageData()
    {
        if (stages != null && currentStageId >= 0 && currentStageId < stages.Length)
        {
            return stages[currentStageId];
        }
        return null; // Return null if the stage is invalid // 如果阶段无效，则返回 null
    }
}
