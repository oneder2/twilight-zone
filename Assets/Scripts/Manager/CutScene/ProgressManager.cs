// File: Scripts/Manager/ProgressManager.cs
using UnityEngine;

/// <summary>
/// Manages and stores the player's progress through the game loops or key events.
/// 管理并存储玩家在游戏循环或关键事件中的进度。
/// Assumes Singleton pattern and persistence ("Boot" scene).
/// 假设使用单例模式并具有持久性（“Boot”场景）。
/// </summary>
public class ProgressManager : Singleton<ProgressManager>
{
    // --- Example Progress Variables ---
    // --- 示例进度变量 ---

    [Tooltip("How many full loops the player has completed.")]
    // [Tooltip("玩家已完成的完整循环次数。")]
    [SerializeField] // Exposed for debugging in Inspector // 为方便在 Inspector 中调试而公开
    private int loopsCompleted = 0;

    // Add more flags or variables as needed for your game's specific conditions
    // 根据游戏的具体条件需要添加更多标志或变量
    // public bool HasKilledFriendNicely { get; private set; } = false;
    // public bool HasFoundSecretEvidence { get; private set; } = false;

    // --- Public Accessors ---
    // --- 公共访问器 ---

    /// <summary>
    /// Gets the number of game loops the player has successfully completed.
    /// 获取玩家已成功完成的游戏循环次数。
    /// </summary>
    public int LoopsCompleted => loopsCompleted;

    // --- Public Modifiers ---
    // --- 公共修改器 ---

    /// <summary>
    /// Increments the loop completion counter. Call this when a loop is successfully finished.
    /// 增加循环完成计数器。在循环成功完成时调用此方法。
    /// </summary>
    public void CompleteLoop()
    {
        loopsCompleted++;
        Debug.Log($"[ProgressManager] Loop completed! Total loops: {loopsCompleted}");
        // Optional: Trigger an event when a loop is completed
        // 可选：在循环完成时触发事件
        // EventManager.Instance?.TriggerEvent(new LoopCompletedEvent(loopsCompleted));
    }

    /// <summary>
    /// Resets all progress tracking variables back to their initial state.
    /// 将所有进度跟踪变量重置回其初始状态。
    /// Typically called when starting a completely new game from the main menu.
    /// 通常在从主菜单开始一个全新的游戏时调用。
    /// </summary>
    public void ResetProgress()
    {
        loopsCompleted = 0;
        // Reset other flags here // 在此处重置其他标志
        // HasKilledFriendNicely = false;
        // HasFoundSecretEvidence = false;
        Debug.Log("[ProgressManager] All progress reset to initial state.");
    }

    // Example methods to set specific flags
    // 设置特定标志的示例方法
    // public void SetFriendKilledNicely(bool value) { HasKilledFriendNicely = value; }
    // public void SetSecretEvidenceFound(bool value) { HasFoundSecretEvidence = value; }


    // --- Ensure Reset on New Game ---
    // --- 确保在新游戏时重置 ---
    // We need to make sure ResetProgress is called appropriately.
    // 我们需要确保 ResetProgress 被适当地调用。
    // Option 1: Call it from GameRunManager.StartGameSession
    // 选项 1：从 GameRunManager.StartGameSession 调用
    // Option 2: Listen for a GameStartEvent (if ProgressManager persists independently)
    // 选项 2：监听 GameStartEvent（如果 ProgressManager 独立持久存在）

    // Let's assume GameRunManager will call it for simplicity.
    // 为简单起见，我们假设 GameRunManager 会调用它。

}
