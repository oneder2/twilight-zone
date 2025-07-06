using UnityEngine;
using UnityEngine.Playables; // Required for PlayableDirector / PlayableDirector 所需

/// <summary>
/// Handles interaction logic for the Teacher NPC, involving evidence checks and determining outcome.
/// Inherits from SavableNPC to handle state persistence.
/// 处理 Teacher NPC 的交互逻辑，涉及证据检查和结局确定。
/// 继承自 SavableNPC 以处理状态持久性。
/// </summary>
public class TeacherNPC : SavableNPC // Inherit from SavableNPC / 继承自 SavableNPC
{
    [Header("Teacher Settings / Teacher 设置")]
    // [Tooltip("Dialogue sequence triggered on interaction (Placeholder - complex choices needed).\n交互时触发的对话序列（占位符 - 需要复杂选择）。")]
    // [SerializeField] private string[] initialDialogue; // Placeholder - REMOVED / 占位符 - 已移除

    [Tooltip("PlayableDirector for the 'Teacher Suicide' outcome.\n'教师自杀' 结局的 PlayableDirector。")]
    [SerializeField] private PlayableDirector suicideOutcomeDirector;

    [Tooltip("PlayableDirector for the 'Player Kills Teacher' outcome.\n'玩家杀死教师' 结局的 PlayableDirector。")]
    [SerializeField] private PlayableDirector killOutcomeDirector;

    private bool interactionComplete = false; // Tracks if interaction has happened / 跟踪交互是否已发生

    // Start is called before the first frame update
    // Start 在第一帧更新之前被调用
    protected override void Start()
    {
        base.Start(); // Calls SavableNPC.Start() for registration / 调用 SavableNPC.Start() 进行注册
        interactionComplete = false;
    }

    /// <summary>
    /// Handles player interaction with the Teacher NPC. Determines outcome based on evidence flags.
    /// 处理玩家与 Teacher NPC 的交互。根据证据标志确定结局。
    /// </summary>
    public override void Interact()
    {
        if (interactionComplete) return; // Prevent re-interaction / 阻止重复交互

        // Check if Teacher is the current target / 检查 Teacher 是否是当前目标
        if (ProgressManager.Instance == null) {
             Debug.LogError("[TeacherNPC] ProgressManager not found!");
             return;
        }
        if (ProgressManager.Instance.CurrentMainTarget != "Teacher")
        {
             if (DialogueManager.Instance != null)
                 DialogueManager.Instance.ShowBlockingDialogue("..."); // Placeholder feedback / 占位符反馈
            Debug.Log("[TeacherNPC] Interaction attempted, but Teacher is not the current target.");
            return;
        }

        Debug.Log("[TeacherNPC] Interaction started.");
        interactionComplete = true; // Mark as complete / 标记为已完成

        // --- REMOVED Placeholder Dialogue Trigger ---
        // --- 移除占位符对话触发 ---
        // Complex dialogue/choices would happen here in a full implementation.
        // 在完整实现中，复杂的对话/选择会在这里发生。
        // For now, we directly check flags.
        // 现在，我们直接检查标志。

        // --- Determine Outcome (Based on evidence flags) ---
        // --- 确定结局（基于证据标志）---
        CharacterOutcome outcome;
        PlayableDirector directorToPlay;

        // Check flags from ProgressManager
        // 检查 ProgressManager 中的标志
        bool evidenceHid = ProgressManager.Instance.HidTeacherEvidence;
        bool evidenceCheckedCorrectly = ProgressManager.Instance.CheckedTeacherEvidenceCorrectly;

        // Define conditions for different outcomes
        // 定义不同结局的条件
        // Example: Player needs to have hidden the evidence AND checked it correctly to get the 'kill' outcome.
        // 示例：玩家需要隐藏证据并正确检查它才能获得“击杀”结局。
        bool killConditionMet = evidenceHid && evidenceCheckedCorrectly;

        if (!killConditionMet) // Failed evidence check or didn't handle it properly / 证据检查失败或未正确处理
        {
            outcome = CharacterOutcome.Suicide;
            directorToPlay = suicideOutcomeDirector;
            Debug.Log("[TeacherNPC] Outcome determined: Suicide (Evidence conditions not met)");
        }
        else // Succeeded / 成功
        {
            outcome = CharacterOutcome.KilledStandard; // Player gets approval and kills / 玩家获得认可并击杀
            directorToPlay = killOutcomeDirector;
            Debug.Log("[TeacherNPC] Outcome determined: Player Kill (Evidence conditions met)");
        }
        // --- End Outcome Determination / 结束结局确定 ---


        // Update Progress Manager / 更新 Progress Manager
        ProgressManager.Instance.SetCharacterOutcome("Teacher", outcome);
        // Only advance target if Teacher didn't commit suicide
        // 仅当 Teacher 没有自杀时才推进目标
        if (outcome != CharacterOutcome.Suicide)
        {
             ProgressManager.Instance.SetCurrentMainTarget("Final"); // Move to next target (Final stage) / 移动到下一个目标（最终阶段）
        } else {
             Debug.Log("[TeacherNPC] Teacher suicide outcome, target remains Teacher (loop might reset).");
             // Consider if the loop should restart here or if Game Over occurs
             // 考虑循环是否应在此处重新开始，或者是否发生游戏结束
        }


        // Trigger the appropriate Timeline sequence / 触发相应的 Timeline 序列
        if (directorToPlay != null)
        {
            Debug.Log($"[TeacherNPC] Playing director: {directorToPlay.name}");
            directorToPlay.Play();
        }
        else
        {
            Debug.LogError($"[TeacherNPC] Director for outcome {outcome} is not assigned!", this);
        }

        // Trigger Saved Event / 触发 Saved 事件
        EventManager.Instance?.TriggerEvent(new TargetSavedEvent("Teacher", outcome));

        // --- Deactivation Logic ---
        // --- 停用逻辑 ---
        // If the player kills the teacher, deactivate the NPC object via code after triggering events.
        // 如果玩家杀死教师，则在触发事件后通过代码停用 NPC 对象。
        // If the teacher commits suicide, the suicide timeline should ideally handle disabling the object via an Activation Track.
        // 如果教师自杀，自杀时间轴最好通过 Activation Track 处理禁用对象。
        // However, we still need to notify the GameSceneManager about the intended final state (inactive).
        // 但是，我们仍然需要通知 GameSceneManager 预期的最终状态（非活动）。
        NotifySavedStateChange(false); // Notify manager the final state should be inactive / 通知管理器最终状态应为非活动
        if (outcome == CharacterOutcome.KilledStandard && directorToPlay == null) // If player kills AND no director handles it
        {
             // Fallback if kill director is missing or doesn't handle deactivation
             // 如果击杀导演丢失或不处理停用，则回退
             Debug.LogWarning("[TeacherNPC] Kill director missing or doesn't handle deactivation. Deactivating manually.");
             gameObject.SetActive(false);
        } else if (outcome == CharacterOutcome.KilledStandard && directorToPlay != null) {
             Debug.Log("[TeacherNPC] Kill director assigned. Assuming it handles deactivation.");
        } else if (outcome == CharacterOutcome.Suicide) {
             Debug.Log("[TeacherNPC] Suicide outcome. Assuming suicide director handles deactivation.");
        }
        // --- End Deactivation Logic ---
    }

    /// <summary>
    /// Provides context-sensitive interaction prompt text.
    /// 提供上下文相关的交互提示文本。
    /// </summary>
     public override string GetDialogue()
    {
        if (!interactionComplete && ProgressManager.Instance != null && ProgressManager.Instance.CurrentMainTarget == "Teacher")
        {
            return "按 E 与 Teacher 对话 (Press E to talk to Teacher)";
        }
        return ""; // No prompt if already interacted or not the target / 如果已交互或不是目标则无提示
    }
}
