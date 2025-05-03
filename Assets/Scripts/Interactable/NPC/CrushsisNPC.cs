using UnityEngine;
using UnityEngine.Playables; // Required for PlayableDirector / PlayableDirector 所需

/// <summary>
/// Handles interaction logic for the Crushsis NPC.
/// Inherits from SavableNPC to handle state persistence.
/// 处理 Crushsis NPC 的交互逻辑。
/// 继承自 SavableNPC 以处理状态持久性。
/// </summary>
public class CrushsisNPC : SavableNPC // Inherit from SavableNPC / 继承自 SavableNPC
{
    [Header("Crushsis Settings / Crushsis 设置")]
    [Tooltip("Dialogue lines before the outcome.\n结局之前的对话行。")]
    [SerializeField] private string[] interactionDialogue;

    [Tooltip("PlayableDirector for the 'Pushed' outcome sequence.\n'推下' 结局序列的 PlayableDirector。")]
    [SerializeField] private PlayableDirector pushedOutcomeDirector;

    [Tooltip("PlayableDirector for the 'Jumped' outcome sequence.\n'跳下' 结局序列的 PlayableDirector。")]
    [SerializeField] private PlayableDirector jumpedOutcomeDirector;

    private bool interactionComplete = false; // Tracks if interaction has happened / 跟踪交互是否已发生

    // Start is called before the first frame update
    // Start 在第一帧更新之前被调用
    protected override void Start()
    {
        base.Start(); // Calls SavableNPC.Start() for registration / 调用 SavableNPC.Start() 进行注册
        interactionComplete = false;
    }

    /// <summary>
    /// Handles player interaction with Crushsis.
    /// 处理玩家与 Crushsis 的交互。
    /// </summary>
    public override void Interact()
    {
        if (interactionComplete) return; // Prevent re-interaction / 阻止重复交互

        // Check if Crushsis is the current target / 检查 Crushsis 是否是当前目标
        if (ProgressManager.Instance == null || ProgressManager.Instance.CurrentMainTarget != "Crushsis")
        {
            if (DialogueManager.Instance != null)
                 DialogueManager.Instance.ShowBlockingDialogue("..."); // Placeholder feedback / 占位符反馈
            // Debug.Log("[CrushsisNPC] Interaction attempted, but Crushsis is not the current target.");
            return;
        }

        // Debug.Log("[CrushsisNPC] Interaction started.");
        interactionComplete = true; // Mark as complete / 标记为已完成

        // Optional: Show pre-outcome dialogue (blocking) / 可选：显示结局前对话（阻塞式）
        if (DialogueManager.Instance != null && interactionDialogue.Length > 0)
        {
            DialogueManager.Instance.ShowBlockingDialogue(interactionDialogue);
            // Ideally wait for dialogue completion here / 理想情况下在此处等待对话完成
        }

        // --- Determine Outcome (Simple Example: Based on Loop Count) ---
        // --- 确定结局（简单示例：基于循环次数）---
        // TODO: Replace this with actual decision logic if needed / TODO: 如果需要，用实际决策逻辑替换
        CharacterOutcome outcome;
        PlayableDirector directorToPlay;

        // Example: First loop Pushed (Alternate), subsequent loops Jumped (Standard)
        // 示例：第一次循环推下 (Alternate)，后续循环跳下 (Standard)
        if (ProgressManager.Instance.LoopsCompleted == 0)
        {
            outcome = CharacterOutcome.KilledAlternate; // Using Alternate for "Pushed" / 使用 Alternate 代表 "Pushed"
            directorToPlay = pushedOutcomeDirector;
            // Debug.Log("[CrushsisNPC] Outcome determined: Pushed (Loop 0)");
        }
        else
        {
            outcome = CharacterOutcome.KilledStandard; // Using Standard for "Jumped" / 使用 Standard 代表 "Jumped"
            directorToPlay = jumpedOutcomeDirector;
            // Debug.Log($"[CrushsisNPC] Outcome determined: Jumped (Loop {ProgressManager.Instance.LoopsCompleted})");
        }
        // --- End Outcome Determination / 结束结局确定 ---

        // Update Progress Manager / 更新 Progress Manager
        ProgressManager.Instance.SetCharacterOutcome("Crushsis", outcome);
        ProgressManager.Instance.SetCurrentMainTarget("Friend"); // Move to next target / 移动到下一个目标

        // Trigger the appropriate Timeline sequence / 触发相应的 Timeline 序列
        if (directorToPlay != null)
        {
            directorToPlay.Play();
        }
        else
        {
            Debug.LogError($"[CrushsisNPC] Director for outcome {outcome} is not assigned!", this);
        }

        // Trigger Saved Event (carries the outcome for consequences)
        // 触发 Saved 事件（携带结局以产生后果）
        EventManager.Instance?.TriggerEvent(new TargetSavedEvent("Crushsis", outcome));

        // --- Deactivate using code AFTER triggering events/timeline ---
        // --- 在触发事件/时间轴后使用代码停用 ---
        DeactivateAndSaveState(); // This notifies manager and sets inactive / 这会通知管理器并设置为非活动
        // --- End Deactivation / 结束停用 ---
    }

    /// <summary>
    /// Provides context-sensitive interaction prompt text.
    /// 提供上下文相关的交互提示文本。
    /// </summary>
    public override string GetDialogue()
    {
        if (!interactionComplete && ProgressManager.Instance != null && ProgressManager.Instance.CurrentMainTarget == "Crushsis")
        {
            return "按 E 与 Crushsis 交互 (Press E to interact with Crushsis)";
        }
        return ""; // No prompt if already interacted or not the target / 如果已交互或不是目标则无提示
    }
}
