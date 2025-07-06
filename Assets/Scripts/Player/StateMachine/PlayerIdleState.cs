using UnityEngine;

/// <summary>
/// Represents the player's idle state. Transitions to walking if movement input is detected.
/// 代表玩家的空闲状态。如果检测到移动输入，则转换为行走状态。
/// </summary>
public class PlayerIdleState : PlayerState
{
    /// <summary>
    /// Constructor for the idle state.
    /// 空闲状态的构造函数。
    /// </summary>
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
        : base(_player, _stateMachine, _animBoolName) { }

    /// <summary>
    /// Called when entering the idle state. Stops player movement.
    /// 进入空闲状态时调用。停止玩家移动。
    /// </summary>
    public override void Enter()
    {
        base.Enter();
        player.ZeroVelocity(); // Ensure player stops when entering idle / 确保玩家进入空闲时停止
    }

    /// <summary>
    /// Called when exiting the idle state.
    /// 退出空闲状态时调用。
    /// </summary>
    public override void Exit()
    {
        base.Exit();
    }

    /// <summary>
    /// Called every frame while in the idle state. Checks for movement input to transition.
    /// 在空闲状态下每帧调用。检查移动输入以进行转换。
    /// </summary>
    public override void Update()
    {
        // First, call base.Update() to read the input values into xInput and yInput
        // 首先，调用 base.Update() 将输入值读入 xInput 和 yInput
        base.Update();

        // --- DETAILED DEBUG LOG ---
        // --- 详细调试日志 ---
        // Log the state and input values *before* the check / 在检查*之前*记录状态和输入值
        // Debug.Log($"[PlayerIdleState Update] Checking for transition. xInput={xInput:F2}, yInput={yInput:F2}, isInputDisabled={player.IsInputDisabled}");
        // --- END DEBUG LOG ---

        // Check if there is movement input AND input is not disabled
        // 检查是否有移动输入且输入未被禁用
        // Note: The check for player.IsInputDisabled might be redundant if base.Update already zeroes input when disabled, but it adds safety.
        // 注意：如果 base.Update 在禁用时已将输入归零，则检查 player.IsInputDisabled 可能冗余，但增加了安全性。
        if ((xInput != 0 || yInput != 0) && !player.IsInputDisabled)
        {
            // --- DETAILED DEBUG LOG ---
            // Debug.Log($"[PlayerIdleState Update] ---> Input detected! Attempting state change to Walk/Exhausted.");
            // --- END DEBUG LOG ---

            // Transition to Exhausted state if marker is set, otherwise Walk
            // 如果设置了力竭标记，则转换为 Exhausted 状态，否则转换为 Walk
            if (player.exhaustMarker)
            {
                stateMachine.ChangeState(player.exhaustedState);
            }
            else
            {
                stateMachine.ChangeState(player.walkState);
            }
        }
        // No else needed, stay in Idle if no input / 如果没有输入则保持 Idle 状态，无需 else
    }
}
