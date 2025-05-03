using UnityEngine;
using UnityEngine.Playables; // Required for PlayableDirector / PlayableDirector 所需
using System.Collections; // Required for Coroutines / Coroutine 所需

/// <summary>
/// Handles interaction logic for the Beginner NPC, including the initial timer and forced kill sequence.
/// Inherits from SavableNPC to handle state persistence.
/// 处理 Beginner NPC 的交互逻辑，包括初始计时器和强制击杀序列。
/// 继承自 SavableNPC 以处理状态持久性。
/// </summary>
public class BeginnerNPC : SavableNPC // Inherit from SavableNPC / 继承自 SavableNPC
{
    [Header("Beginner Settings / Beginner 设置")]
    [Tooltip("Dialogue lines to show on initial interaction.\n初始交互时显示的对话行。")]
    [SerializeField] private string[] initialDialogueLines;

    [Tooltip("Dialogue lines to show after the kill interaction is enabled.\n启用击杀交互后显示的对话行。")]
    [SerializeField] private string[] killPromptDialogue = {"(You feel compelled to act...)"}; // Example / 示例

    [Tooltip("Dialogue lines to show in subsequent loops.\n在后续循环中显示的对话行。")]
    [SerializeField] private string[] loopDialogueLines; // For feedback based on ProgressManager / 用于基于 ProgressManager 的反馈

    [Tooltip("Duration of the initial timer in seconds.\n初始计时器的持续时间（秒）。")]
    [SerializeField] private float timerDuration = 60.0f;

    [Tooltip("PlayableDirector for the Beginner's death sequence.\nBeginner 死亡序列的 PlayableDirector。")]
    [SerializeField] private PlayableDirector deathSequenceDirector;

    [Tooltip("UI Text element to display the timer (optional).\n用于显示计时器的 UI Text 元素（可选）。")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText; // Use TextMeshPro for better text / 使用 TextMeshPro 以获得更好的文本效果

    private bool interactionPossible = true; // Can the player interact? / 玩家是否可以交互？
    private bool killInteractionEnabled = false; // Has the kill prompt been shown? / 击杀提示是否已显示？
    private Coroutine timerCoroutine = null; // Reference to the timer coroutine / 对计时器协程的引用
    private float currentTime = 0; // Current time left on the timer / 计时器剩余当前时间

    // Start is called before the first frame update
    // Start 在第一帧更新之前被调用
    protected override void Start()
    {
        base.Start(); // Calls SavableNPC.Start() for registration / 调用 SavableNPC.Start() 进行注册
        killInteractionEnabled = false;
        interactionPossible = true;
        if (timerText) timerText.gameObject.SetActive(false); // Hide timer initially / 初始隐藏计时器
    }

    /// <summary>
    /// Handles player interaction with the Beginner NPC.
    /// 处理玩家与 Beginner NPC 的交互。
    /// </summary>
    public override void Interact()
    {
        if (!interactionPossible) return; // Exit if interaction is currently blocked / 如果交互当前被阻止则退出

        // --- First Interaction (Loop 0) / 首次交互（循环 0）---
        if (ProgressManager.Instance != null && ProgressManager.Instance.LoopsCompleted == 0 && !killInteractionEnabled)
        {
            // Debug.Log("[BeginnerNPC] Initial interaction.");
            interactionPossible = false; // Prevent interaction during dialogue/timer / 在对话/计时器期间阻止交互

            // Show initial dialogue (blocking) / 显示初始对话（阻塞式）
            if (DialogueManager.Instance != null && initialDialogueLines.Length > 0)
            {
                DialogueManager.Instance.ShowBlockingDialogue(initialDialogueLines);
            }

            // Start the timer / 启动计时器
            StartTimer();

            // Enable the kill interaction possibility AFTER dialogue (or immediately if no dialogue)
            // 在对话之后（或者如果没有对话则立即）启用击杀交互可能性
            StartCoroutine(EnableKillInteractionAfterDelay(1.0f)); // Small delay / 短暂延迟
        }
        // --- Kill Interaction / 击杀交互 ---
        else if (killInteractionEnabled)
        {
            // Debug.Log("[BeginnerNPC] Kill interaction triggered.");
            interactionPossible = false; // Disable further interaction / 禁用进一步交互
            StopTimer(); // Stop timer if running / 如果计时器正在运行则停止

            // Update Progress Manager / 更新 Progress Manager
            if (ProgressManager.Instance != null)
            {
                ProgressManager.Instance.SetCharacterOutcome("Beginner", CharacterOutcome.KilledStandard);
                ProgressManager.Instance.SetCurrentMainTarget("Crushsis"); // Move to next target / 移动到下一个目标
            }

            // Trigger death sequence Timeline (optional visual) / 触发死亡序列 Timeline（可选视觉效果）
            if (deathSequenceDirector != null)
            {
                deathSequenceDirector.Play();
                // Timeline can play animation, sound, etc. / Timeline 可以播放动画、声音等。
            }
            else
            {
                Debug.LogWarning("[BeginnerNPC] Death Sequence Director is not assigned! NPC will just disappear.", this);
            }

            // Trigger Saved Event (Still important for StageManager) / 触发 Saved 事件（对 StageManager 仍然重要）
            EventManager.Instance?.TriggerEvent(new TargetSavedEvent("Beginner", CharacterOutcome.KilledStandard));

            // --- Deactivate using code AFTER triggering events/timeline ---
            // --- 在触发事件/时间轴后使用代码停用 ---
            DeactivateAndSaveState(); // This notifies manager and sets inactive / 这会通知管理器并设置为非活动
            // --- End Deactivation / 结束停用 ---
        }
        // --- Subsequent Loop Interaction / 后续循环交互 ---
        else if (ProgressManager.Instance != null && ProgressManager.Instance.LoopsCompleted > 0)
        {
             // Debug.Log("[BeginnerNPC] Subsequent loop interaction.");
             // Show feedback dialogue based on progress / 根据进度显示反馈对话
             if (DialogueManager.Instance != null && loopDialogueLines.Length > 0)
             {
                  // TODO: Add logic to select specific lines based on ProgressManager flags
                  // TODO: 添加逻辑以根据 ProgressManager 标志选择特定行
                  DialogueManager.Instance.ShowBlockingDialogue(loopDialogueLines); // Use blocking / 使用阻塞式
             }
        }
    }

    /// <summary>
    /// Enables the kill interaction after a short delay.
    /// 在短暂延迟后启用击杀交互。
    /// </summary>
    private IEnumerator EnableKillInteractionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for dialogue to likely show / 等待对话可能显示出来
        killInteractionEnabled = true;
        interactionPossible = true; // Re-enable interaction for the kill prompt / 为击杀提示重新启用交互
        // Debug.Log("[BeginnerNPC] Kill interaction enabled.");
    }

    /// <summary>
    /// Starts the countdown timer.
    /// 启动倒数计时器。
    /// </summary>
     private void StartTimer()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentTime = timerDuration;
        if (timerText) timerText.gameObject.SetActive(true);
        timerCoroutine = StartCoroutine(TimerCoroutine());
        // Debug.Log("[BeginnerNPC] Timer started.");
    }

    /// <summary>
    /// Stops the countdown timer.
    /// 停止倒数计时器。
    /// </summary>
    private void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            // Debug.Log("[BeginnerNPC] Timer stopped.");
        }
         if (timerText) timerText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Coroutine managing the timer countdown and triggering game over if time runs out.
    /// 管理计时器倒计时并在时间耗尽时触发游戏结束的协程。
    /// </summary>
    private IEnumerator TimerCoroutine()
    {
        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (timerText != null)
            {
                // Format time (e.g., 00:59) / 格式化时间（例如，00:59）
                System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(currentTime);
                timerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            }
            yield return null;
        }

        // Timer reached zero / 计时器归零
        currentTime = 0;
        if (timerText != null) timerText.text = "00:00";
        // Debug.Log("[BeginnerNPC] Timer ran out!");
        interactionPossible = false;
        killInteractionEnabled = false; // Prevent kill after timer runs out / 计时器耗尽后阻止击杀

        // Trigger Game Over / 触发游戏结束
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus != GameStatus.GameOver)
        {
            GameRunManager.Instance.ChangeGameStatus(GameStatus.GameOver);
        }
    }

    /// <summary>
    /// Provides context-sensitive interaction prompt text.
    /// 提供上下文相关的交互提示文本。
    /// </summary>
    public override string GetDialogue()
    {
        if (ProgressManager.Instance != null && ProgressManager.Instance.LoopsCompleted == 0 && killInteractionEnabled)
        {
            return "按 E 行动 (Press E to act)"; // Kill prompt / 击杀提示
        }
        else if (ProgressManager.Instance != null && ProgressManager.Instance.LoopsCompleted == 0 && interactionPossible)
        {
            return "按 E 与 Beginner 对话 (Press E to talk to Beginner)"; // Initial prompt / 初始提示
        }
        else if (ProgressManager.Instance != null && ProgressManager.Instance.LoopsCompleted > 0 && interactionPossible)
        {
             return "按 E 与 Beginner 对话 (Press E to talk to Beginner)"; // Subsequent loop prompt / 后续循环提示
        }
        return ""; // No interaction prompt if not possible / 如果不可能则无交互提示
    }
}
