using System.Collections;
using UnityEngine;

/// <summary>
/// An interactable item that displays different dialogue based on whether it has been checked before.
/// Can also update ProgressManager flags if designated as a clue or evidence.
/// Handles special logic for the Crush clue note, including triggering a transition.
/// 可交互物品，根据之前是否检查过显示不同的对话。
/// 如果被指定为线索或证据，还可以更新 ProgressManager 标志。
/// 处理 Crush 线索笔记的特殊逻辑，包括触发转换。
/// </summary>
public class ItemCheckUp : Item // Inherits uniqueID and ownerSceneManager registration from Item / 继承自 Item 的 uniqueID 和 ownerSceneManager 注册
{
    [Header("CheckUp Settings")]
    [Tooltip("Tracks if this item has been checked/interacted with. Saved/Loaded by GameSceneManager.\n跟踪此物品是否已被检查/交互。由 GameSceneManager 保存/加载。")]
    public bool hasBeenChecked = false;

    [Header("Clue/Evidence Link (Optional)")]
    [Tooltip("If this item represents specific evidence or the Crush clue, set the corresponding ProgressManager flag name here (e.g., 'HidTeacherEvidence', 'FoundCrushClue'). Leave empty if not linked.\n如果此物品代表特定证据或 Crush 线索，请在此处设置相应的 ProgressManager 标志名称（例如，'HidTeacherEvidence'，'FoundCrushClue'）。如果未链接则留空。")]
    [SerializeField] private string progressFlagToSet = ""; // e.g., "HidTeacherEvidence", "FoundCrushClue" / 例如 "HidTeacherEvidence", "FoundCrushClue"
    [Tooltip("The boolean value to set the flag to when interacted with (usually true).\n交互时将标志设置为何布尔值（通常为 true）。")]
    [SerializeField] private bool flagValueToSet = true;

    // --- Reference to the Crush Clue Note ID Constant ---
    private const string CRUSH_CLUE_NOTE_ID = "Crush_ClueNote";


    public override void Interact()
    {
        Debug.Log($"[ItemCheckUp Interact] Called on '{uniqueID}' (GameObject: {gameObject.name}). hasBeenChecked: {hasBeenChecked}");

        string[] linesToShow;
        bool isCrushClue = (this.uniqueID == CRUSH_CLUE_NOTE_ID);
        bool triggerTransition = false; // Flag to trigger transition after dialogue / 标记是否在对话后触发转换
        string targetSceneName = null; // Scene to transition to / 要转换到的场景
        string targetTeleporterID = null; // Target teleporter ID (Crush's uniqueID) / 目标传送器 ID (Crush 的 uniqueID)

        // --- Special Handling for Crush Clue Note ---
        if (isCrushClue)
        {
            Debug.Log($"[ItemCheckUp Interact] Matched CRUSH_CLUE_NOTE_ID.");
            string location = "an unknown place";
            string locationID = "";
            if (ProgressManager.Instance != null)
            {
                locationID = ProgressManager.Instance.CurrentCrushLocationID;
                location = locationID; // Display the ID for now, or map to a display name / 暂时显示 ID，或映射到显示名称
                if (string.IsNullOrEmpty(location)) location = "an unknown place";
            } else { Debug.LogError($"[ItemCheckUp Interact] ProgressManager.Instance is NULL!"); }

            linesToShow = new string[] { $"She's not here... the note says she went to the '{location}'." };

            if (!hasBeenChecked)
            {
                hasBeenChecked = true;
                Debug.Log($"[ItemCheckUp Interact] '{uniqueID}' marked as checked.");
                if (!string.IsNullOrEmpty(progressFlagToSet))
                {
                    Debug.Log($"[ItemCheckUp Interact] Calling SetProgressFlag for '{progressFlagToSet}' with value '{flagValueToSet}'.");
                    SetProgressFlag(progressFlagToSet, flagValueToSet); // Sets FoundCrushClue / 设置 FoundCrushClue
                } else { Debug.LogWarning($"[ItemCheckUp] Crush Clue Note '{uniqueID}' is missing 'progressFlagToSet' configuration!"); }

                // --- Prepare for Transition ---
                // --- 准备转换 ---
                if (ProgressManager.Instance != null && !string.IsNullOrEmpty(locationID))
                {
                    targetSceneName = ProgressManager.Instance.GetSceneNameForCrushLocation(locationID);
                    targetTeleporterID = locationID; // Use Crush's unique ID as the target "teleporter" ID / 使用 Crush 的 uniqueID 作为目标“传送器” ID
                    if (!string.IsNullOrEmpty(targetSceneName))
                    {
                        triggerTransition = true;
                        Debug.Log($"[ItemCheckUp Interact] Prepared transition to scene '{targetSceneName}' with target ID '{targetTeleporterID}'.");
                    } else { Debug.LogError($"[ItemCheckUp Interact] Could not get scene name for location ID '{locationID}'!"); }
                }
                // --- End Prepare for Transition ---
            }
        }
        // --- Regular ItemCheckUp Logic ---
        else
        {
            if (!hasBeenChecked)
            {
                linesToShow = itemData.commends;
                hasBeenChecked = true;
                Debug.Log($"[ItemCheckUp] Item '{uniqueID}' marked as checked.");
                if (!string.IsNullOrEmpty(progressFlagToSet))
                {
                     Debug.Log($"[ItemCheckUp Interact] Calling SetProgressFlag for '{progressFlagToSet}' with value '{flagValueToSet}'.");
                     SetProgressFlag(progressFlagToSet, flagValueToSet);
                }
            }
            else
            {
                linesToShow = new string[] { itemData.discribe };
            }
        }

        // Show Dialogue
        // 显示对话
        if (DialogueManager.Instance != null)
        {
             DialogueManager.Instance.ShowBlockingDialogue(linesToShow);
             // --- IMPORTANT: Trigger transition AFTER dialogue if needed ---
             // --- 重要：如果需要，在对话之后触发转换 ---
             if (triggerTransition && !string.IsNullOrEmpty(targetSceneName) && !string.IsNullOrEmpty(targetTeleporterID))
             {
                  // We need to wait until the blocking dialogue is closed.
                  // 我们需要等到阻塞对话关闭。
                  // DialogueManager doesn't have a direct callback for this easily accessible here.
                  // DialogueManager 没有一个可以在这里轻松访问的回调。
                  // Quick Solution: Start a coroutine to wait a bit after dialogue starts.
                  // 快速解决方案：启动一个协程，在对话开始后稍等片刻。
                  // Better Solution: Modify DialogueManager to have an Action event on hide.
                  // 更好的解决方案：修改 DialogueManager，使其在隐藏时有一个 Action 事件。
                  StartCoroutine(TriggerTransitionAfterDialogue(targetSceneName, targetTeleporterID));
             }
        } else { Debug.LogError("[ItemCheckUp] DialogueManager instance not found!"); }
    }

    // --- NEW Coroutine to delay transition ---
    // --- 新增协程以延迟转换 ---
    private IEnumerator TriggerTransitionAfterDialogue(string sceneName, string teleporterID)
    {
        // Wait until the dialogue is likely closed (adjust timing as needed)
        // 等到对话可能关闭（根据需要调整时间）
        // This is imperfect, relies on DialogueManager hiding itself.
        // 这并不完美，依赖于 DialogueManager 自行隐藏。
        yield return new WaitUntil(() => DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive);
        yield return new WaitForSeconds(0.1f); // Small extra buffer / 小的额外缓冲

        Debug.Log($"[ItemCheckUp] Dialogue closed. Triggering TransitionRequestedEvent to '{sceneName}', target ID '{teleporterID}'.");
        EventManager.Instance?.TriggerEvent(new TransitionRequestedEvent(sceneName, teleporterID));
    }
    // --- End New Coroutine ---


    /// <summary>
    /// Helper method to set the appropriate flag in ProgressManager.
    /// 辅助方法，用于在 ProgressManager 中设置适当的标志。
    /// </summary>
    private void SetProgressFlag(string flagName, bool value)
    {
        if (ProgressManager.Instance == null) {
             Debug.LogError($"[ItemCheckUp] Cannot set flag '{flagName}', ProgressManager is null!");
             return;
        }
        switch (flagName)
        {
            case "HidTeacherEvidence": ProgressManager.Instance.SetHidTeacherEvidence(value); break;
            case "CheckedTeacherEvidenceCorrectly": ProgressManager.Instance.SetCheckedTeacherEvidenceCorrectly(value); break;
            case "FoundCrushClue": ProgressManager.Instance.SetFoundCrushClue(value); break;
            default: Debug.LogWarning($"[ItemCheckUp] Item '{uniqueID}' tried to set unknown progress flag: '{flagName}'"); break;
        }
    }

     // Method to directly set the checked state, called by GameSceneManager.LoadSaveData
     // 直接设置检查状态的方法，由 GameSceneManager.LoadSaveData 调用
     public void SetCheckedState(bool isChecked)
     {
          this.hasBeenChecked = isChecked;
          Debug.Log($"[ItemCheckUp] Item '{uniqueID}' checked state set to {isChecked} from loaded data.");
     }

    // UseItem logic remains the same
    // UseItem 逻辑保持不变
    public override bool UseItem(ItemData usedItemData)
    {
        if (usedItemData != null && this.itemData != null &&
            usedItemData.itemName == "Key" && this.itemData.itemName == "LockedBox")
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowBlockingDialogue(new string[] { "The box is unlocked!" });
                return true;
            } else { Debug.LogError("[ItemCheckUp] DialogueManager instance not found!"); }
        }
        return false;
    }
}
