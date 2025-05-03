using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Handles interaction logic for the Crush NPC.
/// Assumes the player finds their location via a note (ItemCheckUp).
/// Activation/Deactivation is handled externally by StageManager based on progress.
/// 处理 Crush NPC 的交互逻辑。
/// 假设玩家通过笔记 (ItemCheckUp) 找到他们的位置。
/// 激活/停用由 StageManager 根据进度在外部处理。
/// </summary>
public class CrushNPC : SavableNPC
{
    [Header("Crush Settings")]
    [Tooltip("Dialogue lines upon interaction (e.g., expressing 'mad love').\n交互时的对话行（例如，表达'狂热爱慕'）。")]
    [SerializeField] private string[] interactionDialogue;

    [Tooltip("PlayableDirector for the sequence after killing Crush.\n杀死 Crush 后序列的 PlayableDirector。")]
    [SerializeField] private PlayableDirector postKillDirector; // Optional / 可选

    // Add fields for different outcomes/Timelines if interaction has choices/branches
    // 如果交互有选择/分支，则为不同的结局/时间轴添加字段

    private bool interactionComplete = false;

    protected override void Start()
    {
        base.Start(); // Handles registration / 处理注册
        interactionComplete = false;
        // --- REMOVED self-disabling logic ---
        // --- 移除自我禁用逻辑 ---
        // Activation is now handled by StageManager.ApplyStageSpecificObjectState
        // 激活现在由 StageManager.ApplyStageSpecificObjectState 处理
    }

    public override void Interact()
    {
        if (interactionComplete) return;

        // Check if Crush is the current target
        // 检查 Crush 是否是当前目标
        if (ProgressManager.Instance == null || ProgressManager.Instance.CurrentMainTarget != "Crush")
        {
             if (DialogueManager.Instance != null)
                 DialogueManager.Instance.ShowBlockingDialogue("..."); // Placeholder dialogue / 占位符对话
            Debug.Log("[CrushNPC] Interaction attempted, but Crush is not the current target.");
            return;
        }

        // --- NEW: Additional check - ensure the clue has been found ---
        // --- 新增：额外检查 - 确保线索已被找到 ---
        // This prevents interacting if the player somehow reaches the location without the clue
        // 这可以防止玩家在没有线索的情况下以某种方式到达该位置并进行交互
        if (!ProgressManager.Instance.FoundCrushClue)
        {
             if (DialogueManager.Instance != null)
                 DialogueManager.Instance.ShowBlockingDialogue("You shouldn't be here yet..."); // Or more cryptic message / 或更神秘的消息
             Debug.Log("[CrushNPC] Interaction attempted, but the clue note hasn't been found/checked.");
             return;
        }
        // --- End New Check ---


        Debug.Log($"[CrushNPC] Interaction started with NPC ID: {this.uniqueID}."); // Log which instance / 记录是哪个实例
        interactionComplete = true;

        // Show interaction dialogue
        // 显示交互对话
        if (DialogueManager.Instance != null && interactionDialogue.Length > 0)
        {
            DialogueManager.Instance.ShowBlockingDialogue(interactionDialogue);
            // Need to wait or proceed? Assume proceed for now.
            // 需要等待还是继续？暂时假设继续。
        }

        // --- Determine Outcome ---
        // --- 确定结局 ---
        // TODO: Implement choice mechanism if applicable (e.g., accept/reject advances?)
        // TODO: 如果适用，实现选择机制（例如，接受/拒绝示好？）
        CharacterOutcome outcome = CharacterOutcome.KilledStandard; // Default outcome / 默认结局
        Debug.Log($"[CrushNPC] Outcome determined: {outcome}");

        // Update Progress Manager
        // 更新 Progress Manager
        if (ProgressManager.Instance != null)
        {
             ProgressManager.Instance.SetCharacterOutcome("Crush", outcome);
             ProgressManager.Instance.SetCurrentMainTarget("Teacher"); // Move to next target / 移动到下一个目标
        }

        // Trigger the appropriate post-kill effect/Timeline
        // 触发适当的击杀后效果/时间轴
        if (postKillDirector != null)
        {
            postKillDirector.Play();
        }
        else
        {
            Debug.LogWarning($"[CrushNPC] No post-kill director assigned for NPC '{uniqueID}'. Hiding NPC directly.");
            // Fallback: Deactivate directly if no timeline handles it
            // 回退：如果没有时间轴处理，则直接停用
            DeactivateAndSaveState(); // Use the SavableNPC method / 使用 SavableNPC 方法
        }

        // Trigger Saved Event (carries the outcome for consequences like ghost enhancement)
        // 触发 Saved 事件（携带结局以产生后果，例如幽灵增强）
        EventManager.Instance?.TriggerEvent(new TargetSavedEvent("Crush", outcome));

        // Let the Timeline handle disabling/destroying the NPC object if applicable
        // 如果适用，让 Timeline 处理禁用/销毁 NPC 对象
        // If using DeactivateAndSaveState() above, Timeline activation track might conflict.
        // 如果上面使用了 DeactivateAndSaveState()，Timeline 激活轨道可能会冲突。
        // Best practice: If a timeline plays, let IT handle deactivation via Activation Track,
        // but still call NotifySavedStateChange(false) here. If no timeline, call DeactivateAndSaveState().
        // 最佳实践：如果播放时间轴，让它通过 Activation Track 处理停用，
        // 但仍在此处调用 NotifySavedStateChange(false)。如果没有时间轴，则调用 DeactivateAndSaveState()。
        // For simplicity now, if director exists, we assume it handles deactivation.
        // 为简单起见，现在如果 director 存在，我们假设它处理停用。
        if (postKillDirector != null) {
             NotifySavedStateChange(false); // Notify state change, Timeline handles SetActive(false)
                                            // 通知状态更改，Timeline 处理 SetActive(false)
        }
        // Else case handled above by calling DeactivateAndSaveState()
        // 上面通过调用 DeactivateAndSaveState() 处理了 Else 情况
    }

    public override string GetDialogue()
    {
        // Check ProgressManager exists and is in correct stage AND clue found
        // 检查 ProgressManager 是否存在，是否处于正确阶段，并且线索已找到
        if (!interactionComplete &&
            ProgressManager.Instance != null &&
            ProgressManager.Instance.CurrentMainTarget == "Crush" &&
            ProgressManager.Instance.FoundCrushClue)
        {
            return "按 E 与 Crush 交互 (Press E to interact with Crush)";
        }
        return ""; // No prompt otherwise / 否则无提示
    }
}
