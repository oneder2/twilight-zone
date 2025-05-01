// File: Scripts/Manager/GameStage/StageManager.cs
using UnityEngine;
using System.Collections; // Needed for Coroutines / Coroutine 所需
using System.Collections.Generic; // Needed for Dictionary / Dictionary 所需
using System.Linq; // Needed for LINQ / LINQ 所需
using UnityEngine.SceneManagement; // Needed for Scene operations / Scene 操作所需

/// <summary>
/// Manages the game's progression through different stages.
/// Applies settings (lighting, music, enemies) and handles stage transitions.
/// Switches stage IMMEDIATELY upon receiving TargetSavedEvent.
/// Also handles dynamic activation/deactivation of stage-specific objects like Crush NPC variants.
/// 管理游戏经历不同阶段的进程。
/// 应用设置（光照、音乐、敌人）并处理阶段转换。
/// 在收到 TargetSavedEvent 后立即切换阶段。
/// 同时处理特定阶段对象的动态激活/停用，例如 Crush NPC 的不同变体。
/// </summary>
public class StageManager : Singleton<StageManager>
{
    [Tooltip("Assign all StageData ScriptableObjects here in order.\n请在此处按顺序分配所有 StageData ScriptableObject。")]
    [SerializeField] private StageData[] stages;

    [Tooltip("Time in seconds before automatically advancing to the next stage if the target isn't dealt with (Set <= 0 to disable).\n如果目标未被处理，则在自动前进到下一阶段之前的秒数（设置 <= 0 以禁用）。")]
    [SerializeField] private float autoAdvanceTime = 120f;

    // --- Constants for Crush stage object IDs ---
    // --- Crush 阶段对象 ID 常量 ---
    private const string CRUSH_ORIGINAL_NPC_ID = "Crush_Original"; // Ensure this matches the corrected ID in Inspector / 确保这与 Inspector 中修正后的 ID 匹配
    private const string CRUSH_CLUE_NOTE_ID = "Crush_ClueNote";
    private const string CRUSH_STAGE_NAME = "Crush Stage"; // Use a constant for the stage name check / 使用常量进行阶段名称检查

    private int currentStageId = -1; // Current active stage ID / 当前活动阶段 ID
    // Tracks the unique names of registered auto-advance timed events / 跟踪已注册的自动推进定时事件的唯一名称
    private Dictionary<int, string> stageAutoAdvanceEventNames = new Dictionary<int, string>();

    // Mapping from Target Name (string) to Next Stage ID (int)
    // 从目标名称（字符串）到下一阶段 ID（整数）的映射
    private Dictionary<string, int> targetToNextStageMap = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Beginner", 1 }, // Stage 0 -> Stage 1
        { "Crushsis", 2 }, // Stage 1 -> Stage 2
        { "Friend",   3 }, // Stage 2 -> Stage 3 (Crush Stage)
        { "Crush",    4 }, // Stage 3 -> Stage 4 (Teacher Stage)
        { "Teacher",  5 } // Stage 4 -> Stage 5 (Final Stage)
    };

    // Called when the script instance is being loaded / 在加载脚本实例时调用
    void Start()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<StageChangeEvent>(OnStageChangeRequested);
            EventManager.Instance.AddListener<TargetSavedEvent>(OnTargetSaved);
            Debug.Log("[StageManager] Subscribed to StageChangeEvent and TargetSavedEvent.");
        }
        else { Debug.LogError("[StageManager] EventManager instance not found! Cannot subscribe to events."); }

        if (stages != null && stages.Length > 0)
        {
             StartCoroutine(InitializeFirstStageAfterDelay());
        } else {
             Debug.LogError("[StageManager] Stages array is not assigned or empty!", this);
        }
    }

    // Coroutine for initial setup / 用于初始设置的协程
    private IEnumerator InitializeFirstStageAfterDelay()
    {
        yield return null; // Wait one frame / 等待一帧
        SetStage(0); // Start at stage 0 / 从阶段 0 开始
    }

    // Called when the script instance is being destroyed / 在销毁脚本实例时调用
    void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<StageChangeEvent>(OnStageChangeRequested);
            EventManager.Instance.RemoveListener<TargetSavedEvent>(OnTargetSaved);
        }
        CancelAllAutoAdvanceEvents();
    }

    // --- Event Handlers / 事件处理器 ---
    private void OnStageChangeRequested(StageChangeEvent stageEvent)
    {
        if (stageEvent == null) return;
        Debug.Log($"[StageManager] Received StageChangeEvent request for Stage ID: {stageEvent.StageId}.");
        SetStage(stageEvent.StageId);
    }

    private void OnTargetSaved(TargetSavedEvent savedEvent)
    {
        if (savedEvent == null || string.IsNullOrEmpty(savedEvent.TargetName)) return;
        Debug.Log($"[StageManager] === Received TargetSavedEvent for Target: {savedEvent.TargetName} ===");
        CancelAutoAdvanceEvent(currentStageId);
        if (targetToNextStageMap.TryGetValue(savedEvent.TargetName, out int nextStageId))
        {
            Debug.Log($"[StageManager] Target '{savedEvent.TargetName}' completed. Advancing immediately to Stage ID: {nextStageId}");
            SetStage(nextStageId);
        }
        else
        {
            Debug.LogWarning($"[StageManager] No next stage defined for target '{savedEvent.TargetName}' in targetToNextStageMap.");
             if (savedEvent.TargetName.Equals("Teacher", System.StringComparison.OrdinalIgnoreCase))
             {
                  Debug.Log("[StageManager] Last target 'Teacher' saved. Ending logic should proceed.");
                  if (savedEvent.Outcome != CharacterOutcome.Suicide && ProgressManager.Instance != null)
                  {
                       ProgressManager.Instance.SetCurrentMainTarget("Final");
                  }
             }
        }
    }

    // --- Stage Management Logic / 阶段管理逻辑 ---
    public void SetStage(int stageId)
    {
        if (stages == null || stageId < 0 || stageId >= stages.Length)
        {
            Debug.LogError($"[StageManager] Invalid stage ID requested: {stageId}. Max index is {stages?.Length - 1 ?? -1}.", this);
            return;
        }
        if (currentStageId == stageId)
        {
             Debug.Log($"[StageManager] Already in stage {stageId}. No change needed.");
             return;
        }
        CancelAutoAdvanceEvent(currentStageId);
        int previousStageId = currentStageId;
        currentStageId = stageId;
        Debug.Log($"[StageManager] === Switching from Stage {previousStageId} to Stage ID: {currentStageId} ({stages[currentStageId]?.stageName ?? "Error: Missing StageData"}) ===");

        // Determine Crush location if entering Crush stage
        // 如果进入 Crush 阶段，则确定 Crush 位置
        if (stages[currentStageId].stageName.Equals(CRUSH_STAGE_NAME, System.StringComparison.OrdinalIgnoreCase))
        {
             if(ProgressManager.Instance != null) {
                 ProgressManager.Instance.DetermineCrushLocationForLoop();
             } else {
                 Debug.LogError("[StageManager] ProgressManager is NULL when trying to determine Crush location!");
             }
        }

        ApplyStageSettings();
        StartCoroutine(ApplyStageObjectStateAfterDelay(currentStageId)); // Apply object state after delay / 延迟后应用对象状态

        // Register Auto-Advance timer / 注册自动推进计时器
        int nextStageIdTimed = currentStageId + 1;
        if (nextStageIdTimed < stages.Length && autoAdvanceTime > 0)
        {
            string autoEventName = $"AutoAdvance_Stage_{currentStageId}_To_{nextStageIdTimed}";
            Debug.Log($"[StageManager] Registering auto-advance: {autoEventName} in {autoAdvanceTime}s");
            EventManager.Instance?.RegisterTimeEvent(autoEventName, autoAdvanceTime, new StageChangeEvent(nextStageIdTimed));
            stageAutoAdvanceEventNames[currentStageId] = autoEventName;
        }
        else
        {
             Debug.Log($"[StageManager] Reached last stage ({currentStageId}) or auto-advance disabled. No timer set.");
        }
    }

    private void ApplyStageSettings()
    {
        if (stages == null || currentStageId < 0 || currentStageId >= stages.Length || stages[currentStageId] == null)
        {
            Debug.LogError($"[StageManager] Cannot apply settings. Invalid stage ID ({currentStageId}) or missing StageData.", this);
            return;
        }
        StageData currentStage = stages[currentStageId];
        Debug.Log($"[StageManager] Applying settings for Stage: {currentStage.stageName}");
        LightManager.Instance?.UpdateLighting(currentStage.lightIntensity, currentStage.lightColor);
        AudioManager.Instance?.PlayMusic(currentStage.trackId);
        EnemySpawner.Instance?.ConfigureSpawner(currentStage);
        Debug.Log($"[StageManager] Finished applying settings for Stage: {currentStage.stageName}");
    }

    // --- Coroutine to apply object states after a delay ---
    private IEnumerator ApplyStageObjectStateAfterDelay(int stageIdToApply)
    {
        // yield return null; // Wait one frame
        yield return new WaitForSeconds(0.1f); // Wait for a short duration instead
        // Or: yield return new WaitForEndOfFrame(); // Wait until end of frame rendering
        ApplyStageSpecificObjectState(stageIdToApply);
    }

    // --- Method to handle object activation/deactivation ---
    private void ApplyStageSpecificObjectState(int currentStageId)
    {
        Debug.Log($"[StageManager ApplyObjectState] === Applying object state for Stage ID: {currentStageId} ==="); // DEBUG

        if (stages == null || currentStageId < 0 || currentStageId >= stages.Length || stages[currentStageId] == null)
        {
             Debug.LogError($"[StageManager ApplyObjectState] Invalid stage ID ({currentStageId}) or missing StageData. Aborting."); // DEBUG
             return;
        }

        bool isCrushStage = stages[currentStageId].stageName.Equals(CRUSH_STAGE_NAME, System.StringComparison.OrdinalIgnoreCase);
        Debug.Log($"[StageManager ApplyObjectState] Stage Name: '{stages[currentStageId].stageName}'. Is Crush Stage: {isCrushStage}"); // DEBUG

        // --- DEBUG LOG: List Loaded Scenes ---
        Debug.LogWarning($"[StageManager ApplyObjectState] Checking loaded scenes before FindObjectsByType:");
        for(int i=0; i<SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.LogWarning($"  - Scene {i}: {scene.name} (Loaded: {scene.isLoaded})");
        }
        // --- END DEBUG LOG ---

        // Find objects (Consider optimizing if becomes slow) / 查找对象（如果变慢则考虑优化）
        var allSavableNPCs = FindObjectsByType<SavableNPC>(FindObjectsSortMode.None);
        var allItems = FindObjectsByType<Item>(FindObjectsSortMode.None);
        Debug.Log($"[StageManager ApplyObjectState] Found {allSavableNPCs.Length} SavableNPCs and {allItems.Length} Items."); // DEBUG

        // --- Crush Stage Logic ---
        string targetCrushLocationID = "";
        List<string> possibleLocations = null; // Store possible locations / 存储可能的位置
        if (isCrushStage)
        {
            if (ProgressManager.Instance != null)
            {
                targetCrushLocationID = ProgressManager.Instance.CurrentCrushLocationID;
                possibleLocations = ProgressManager.Instance.possibleCrushLocationIDs; // Get the list / 获取列表
                Debug.Log($"[StageManager ApplyObjectState] Crush Stage active. Target Location ID: '{targetCrushLocationID}'. Possible IDs: [{string.Join(", ", possibleLocations ?? new List<string>())}]"); // DEBUG
            } else {
                 Debug.LogError("[StageManager ApplyObjectState] ProgressManager is NULL during Crush Stage!"); // DEBUG
            }
        }

        // Handle Original Crush NPC (ID: "Crush_Original")
        SavableNPC originalCrush = allSavableNPCs.FirstOrDefault(npc => npc.uniqueID == CRUSH_ORIGINAL_NPC_ID);
        if (originalCrush != null)
        {
            bool shouldOriginalBeActive = !isCrushStage;
            Debug.Log($"[StageManager ApplyObjectState] Found Original Crush ('{CRUSH_ORIGINAL_NPC_ID}'). Should be active: {shouldOriginalBeActive}. Currently: {originalCrush.gameObject.activeSelf}"); // DEBUG
            if (originalCrush.gameObject.activeSelf != shouldOriginalBeActive)
            {
                 Debug.Log($"[StageManager ApplyObjectState] --> Setting '{CRUSH_ORIGINAL_NPC_ID}' active: {shouldOriginalBeActive}"); // DEBUG
                 originalCrush.gameObject.SetActive(shouldOriginalBeActive);
                 // Notify manager ONLY if state changed (SavableNPC handles notification on deactivate)
                 // 仅当状态更改时通知管理器（SavableNPC 在停用时处理通知）
                 if (shouldOriginalBeActive) originalCrush.NotifySavedStateChange(true);
            }
        } else {
             if (!isCrushStage) Debug.LogWarning($"[StageManager ApplyObjectState] Could not find Original Crush NPC: {CRUSH_ORIGINAL_NPC_ID}"); // DEBUG
        }

        // Handle Crush Clue Note (ID: "Crush_ClueNote")
        Item clueNote = allItems.FirstOrDefault(item => item.uniqueID == CRUSH_CLUE_NOTE_ID);
        if (clueNote != null)
        {
            bool shouldNoteBeActive = isCrushStage;
            Debug.Log($"[StageManager ApplyObjectState] Found Clue Note ('{CRUSH_CLUE_NOTE_ID}'). Should be active: {shouldNoteBeActive}. Currently: {clueNote.gameObject.activeSelf}"); // DEBUG
            if (clueNote.gameObject.activeSelf != shouldNoteBeActive)
            {
                 Debug.Log($"[StageManager ApplyObjectState] --> Setting '{CRUSH_CLUE_NOTE_ID}' active: {shouldNoteBeActive}"); // DEBUG
                 clueNote.gameObject.SetActive(shouldNoteBeActive);
            }
        } else {
             if (isCrushStage) Debug.LogWarning($"[StageManager ApplyObjectState] Could not find Clue Note Item: {CRUSH_CLUE_NOTE_ID}"); // DEBUG
        }

        // Handle Target Location Crush NPCs
        if (possibleLocations != null) // Check if list is valid / 检查列表是否有效
        {
            foreach (string possibleLocationID in possibleLocations)
            {
                SavableNPC targetCrush = allSavableNPCs.FirstOrDefault(npc => npc.uniqueID == possibleLocationID);
                if (targetCrush != null)
                {
                    bool shouldTargetBeActive = isCrushStage && possibleLocationID == targetCrushLocationID;
                    Debug.Log($"[StageManager ApplyObjectState] Found Target Crush Candidate ('{possibleLocationID}'). Should be active: {shouldTargetBeActive}. Currently: {targetCrush.gameObject.activeSelf}"); // DEBUG
                    if (targetCrush.gameObject.activeSelf != shouldTargetBeActive)
                    {
                        Debug.Log($"[StageManager ApplyObjectState] --> Setting Target Crush NPC '{possibleLocationID}' active: {shouldTargetBeActive}"); // DEBUG
                        targetCrush.gameObject.SetActive(shouldTargetBeActive);
                        if (shouldTargetBeActive) targetCrush.NotifySavedStateChange(true);
                    }
                } else if (isCrushStage && possibleLocationID == targetCrushLocationID) {
                     Debug.LogWarning($"[StageManager ApplyObjectState] Could not find the *expected* Target Crush NPC: {possibleLocationID}"); // DEBUG
                }
            }
        } else if (isCrushStage) {
             Debug.LogError("[StageManager ApplyObjectState] possibleCrushLocationIDs list is null! Cannot activate target Crush NPC."); // DEBUG
        }
        // --- End Crush Stage Logic ---

        Debug.Log($"[StageManager ApplyObjectState] === Finished applying object state for Stage ID: {currentStageId} ==="); // DEBUG
    }


    // --- Helper Methods / 辅助方法 ---
    private void CancelAutoAdvanceEvent(int stageIdToCancel)
    {
        if (stageIdToCancel != -1 && stageAutoAdvanceEventNames.TryGetValue(stageIdToCancel, out string eventName))
        {
            if (EventManager.Instance != null && EventManager.Instance.CancelTimeEvent(eventName))
            {
                Debug.Log($"[StageManager] Cancelled auto-advance event '{eventName}' for stage {stageIdToCancel}.");
            }
            stageAutoAdvanceEventNames.Remove(stageIdToCancel);
        }
    }

    private void CancelAllAutoAdvanceEvents()
    {
         if (EventManager.Instance != null)
         {
              foreach (var eventName in stageAutoAdvanceEventNames.Values) { EventManager.Instance.CancelTimeEvent(eventName); }
         }
         stageAutoAdvanceEventNames.Clear();
         Debug.Log("[StageManager] Cancelled all tracked auto-advance events.");
    }

    public StageData GetCurrentStageData()
    {
        if (stages != null && currentStageId >= 0 && currentStageId < stages.Length) { return stages[currentStageId]; }
        return null;
    }
}
