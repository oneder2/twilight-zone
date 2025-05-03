using UnityEngine;
using UnityEngine.Playables; // If using Timeline for post-kill effect / 如果使用 Timeline 实现击杀后效果

/// <summary>
/// Handles interaction logic for the Friend NPC, checking for a required key.
/// Inherits from SavableNPC to handle state persistence.
/// 处理 Friend NPC 的交互逻辑，检查所需的钥匙。
/// 继承自 SavableNPC 以处理状态持久性。
/// </summary>
public class FriendNPC : SavableNPC // Inherit from SavableNPC / 继承自 SavableNPC
{
    [Header("Friend Settings / Friend 设置")]
    [Tooltip("Dialogue lines when interaction is attempted without the key.\n没有钥匙时尝试交互显示的对话行。")]
    [SerializeField] private string[] lockedDialogue = {"He seems busy in the lab. It's locked."};

    [Tooltip("Dialogue lines when interaction is successful (before kill).\n交互成功时（击杀前）显示的对话行。")]
    [SerializeField] private string[] successDialogue; // Optional / 可选

    [Tooltip("ItemData representing the key needed to interact.\n交互所需的钥匙的 ItemData。")]
    [SerializeField] private ItemData requiredKey;

    [Tooltip("PlayableDirector for the sequence after killing the friend (optional).\n杀死朋友后序列的 PlayableDirector（可选）。")]
    [SerializeField] private PlayableDirector postKillDirector; // Optional / 可选

    private bool interactionComplete = false; // Tracks if interaction has happened / 跟踪交互是否已发生

    // Start is called before the first frame update
    // Start 在第一帧更新之前被调用
    protected override void Start()
    {
        base.Start(); // Calls SavableNPC.Start() for registration / 调用 SavableNPC.Start() 进行注册
        interactionComplete = false;
    }

    /// <summary>
    /// Handles player interaction with the Friend NPC.
    /// 处理玩家与 Friend NPC 的交互。
    /// </summary>
    public override void Interact()
    {
        if (interactionComplete) return; // Prevent re-interaction / 阻止重复交互

        // Check if Friend is the current target / 检查 Friend 是否是当前目标
        if (ProgressManager.Instance == null || ProgressManager.Instance.CurrentMainTarget != "Friend")
        {
            if (DialogueManager.Instance != null)
                 DialogueManager.Instance.ShowBlockingDialogue("..."); // Placeholder feedback / 占位符反馈
            Debug.Log("[FriendNPC] Interaction attempted, but Friend is not the current target.");
            return;
        }

        // Check for required key / 检查所需钥匙
        bool hasKey = false;
        if (requiredKey != null && Inventory.Instance != null)
        {
            // Check if the specific ItemData instance exists in the inventory list
            // 检查特定的 ItemData 实例是否存在于库存列表中
            hasKey = Inventory.Instance.GetItemDatas().Contains(requiredKey);
            Debug.Log($"[FriendNPC] Checking for key '{requiredKey.itemName}'. Found in inventory: {hasKey}");
        }
        else if (requiredKey == null)
        {
             Debug.LogWarning("[FriendNPC] Required Key ItemData is not assigned. Assuming key is not needed.");
             hasKey = true; // Proceed if no key is specified / 如果未指定钥匙则继续
        }
        else // Inventory is null
        {
            Debug.LogError("[FriendNPC] Inventory.Instance is null! Cannot check for key.");
            return; // Cannot proceed without inventory / 没有库存无法继续
        }


        if (hasKey) // Interaction successful / 交互成功
        {
            Debug.Log("[FriendNPC] Interaction successful (has key).");
            interactionComplete = true;

            // Update Progress Manager (including key flag if not done elsewhere)
            // 更新 Progress Manager（包括钥匙标志，如果在别处未完成）
            if (ProgressManager.Instance != null)
            {
                 if (requiredKey != null && !ProgressManager.Instance.FoundFriendLabKey)
                 {
                      ProgressManager.Instance.SetFoundFriendLabKey(true); // Set flag now / 现在设置标志
                 }
            }

            // Optional: Show success dialogue (blocking) / 可选：显示成功对话（阻塞式）
            if (DialogueManager.Instance != null && successDialogue.Length > 0)
            {
                DialogueManager.Instance.ShowBlockingDialogue(successDialogue);
                // Ideally wait here until dialogue is hidden / 理想情况下在此处等待对话被隐藏
            }

            // --- Determine Outcome ---
            // TODO: Add logic if there are different ways to kill the friend / TODO: 如果有不同的方式杀死朋友，添加逻辑
            CharacterOutcome outcome = CharacterOutcome.KilledStandard; // Default outcome / 默认结局
            Debug.Log($"[FriendNPC] Outcome determined: {outcome}");

            // Update Progress Manager State / 更新 Progress Manager 状态
            if (ProgressManager.Instance != null)
            {
                ProgressManager.Instance.SetCharacterOutcome("Friend", outcome);
                ProgressManager.Instance.SetCurrentMainTarget("Crush"); // Move to next target / 移动到下一个目标
            }

            // Trigger Post-Kill Effect/Timeline / 触发击杀后效果/时间轴
            if (postKillDirector != null)
            {
                postKillDirector.Play();
            }
            else
            {
                Debug.Log("[FriendNPC] No post-kill director assigned.");
            }

            // Trigger Saved Event (carries the outcome for consequences like traps)
            // 触发 Saved 事件（携带结局以产生后果，如陷阱）
            EventManager.Instance?.TriggerEvent(new TargetSavedEvent("Friend", outcome));

            // --- Deactivate using code AFTER triggering events/timeline ---
            // --- 在触发事件/时间轴后使用代码停用 ---
            DeactivateAndSaveState(); // This notifies manager and sets inactive / 这会通知管理器并设置为非活动
            // --- End Deactivation / 结束停用 ---
        }
        else // Missing key / 缺少钥匙
        {
            Debug.Log("[FriendNPC] Interaction failed (missing key).");
            // Show locked dialogue (Blocking) / 显示锁定对话（阻塞式）
            if (DialogueManager.Instance != null && lockedDialogue.Length > 0)
            {
                DialogueManager.Instance.ShowBlockingDialogue(lockedDialogue);
            }
        }
    }

    /// <summary>
    /// Provides context-sensitive interaction prompt text.
    /// 提供上下文相关的交互提示文本。
    /// </summary>
     public override string GetDialogue()
    {
        // Ensure ProgressManager and Inventory exist before checking
        // 在检查之前确保 ProgressManager 和 Inventory 存在
        if (ProgressManager.Instance == null || Inventory.Instance == null) return "";

        if (!interactionComplete && ProgressManager.Instance.CurrentMainTarget == "Friend")
        {
            // Check if key is needed and NOT present / 检查是否需要钥匙且钥匙不存在
            bool keyNeeded = requiredKey != null;
            bool keyPresent = keyNeeded && Inventory.Instance.GetItemDatas().Contains(requiredKey);

            if (keyNeeded && !keyPresent)
            {
                 return "门锁着 (Door is locked)"; // Locked prompt / 锁定提示
            } else {
                 return "按 E 与 Friend 交互 (Press E to interact with Friend)"; // Interact prompt / 交互提示
            }
        }
        return ""; // No prompt if already interacted or not the target / 如果已交互或不是目标则无提示
    }
}
