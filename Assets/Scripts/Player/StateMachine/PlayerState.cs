using UnityEngine;

/// <summary>
/// Base class for all player states in the state machine.
/// Handles basic input reading and common Enter/Exit logic.
/// 状态机中所有玩家状态的基类。
/// 处理基本输入读取和通用的进入/退出逻辑。
/// </summary>
public class PlayerState
{
    protected PlayerStateMachine stateMachine; // Reference to the state machine controller / 对状态机控制器的引用
    protected Player player; // Reference to the main Player script / 对主 Player 脚本的引用
    protected string animBoolName; // Name of the animation boolean parameter for this state / 此状态的动画布尔参数名称

    protected float xInput; // Horizontal input value / 水平输入值
    protected float yInput; // Vertical input value / 垂直输入值

    /// <summary>
    /// Constructor for the player state.
    /// 玩家状态的构造函数。
    /// </summary>
    /// <param name="_player">Reference to the Player script. / 对 Player 脚本的引用。</param>
    /// <param name="_stateMachine">Reference to the PlayerStateMachine. / 对 PlayerStateMachine 的引用。</param>
    /// <param name="_animBoolName">Animation parameter name. / 动画参数名称。</param>
    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    /// <summary>
    /// Called when entering this state. Sets the animation boolean.
    /// 进入此状态时调用。设置动画布尔值。
    /// </summary>
    public virtual void Enter()
    {
        // Ensure player and anim are valid / 确保 player 和 anim 有效
        if (player?.anim != null)
        {
             player.anim.SetBool(animBoolName, true);
        } else {
             // Debug.LogError($"Player or Animator is null in PlayerState.Enter for state: {this.GetType().Name}");
        }
        // Debug.Log($"Entering State: {this.GetType().Name}"); // Log state entry / 记录状态进入
    }

    /// <summary>
    /// Called when exiting this state. Resets the animation boolean.
    /// 退出此状态时调用。重置动画布尔值。
    /// </summary>
    public virtual void Exit()
    {
        if (player?.anim != null)
        {
             player.anim.SetBool(animBoolName, false);
        }
         // Debug.Log($"Exiting State: {this.GetType().Name}"); // Log state exit / 记录状态退出
    }

    /// <summary>
    /// Called every frame while this state is active. Reads input if not disabled.
    /// 此状态活动时每帧调用。如果未禁用则读取输入。
    /// </summary>
    public virtual void Update()
    {
        // Read input ONLY if the Player script allows it (based on GameStatus via isInputDisabled)
        // 仅当 Player 脚本允许时（通过 isInputDisabled 基于 GameStatus）才读取输入
        if (player != null && !player.IsInputDisabled)
        {
            xInput = Input.GetAxisRaw("Horizontal");
            yInput = Input.GetAxisRaw("Vertical");

            // --- DETAILED DEBUG LOG ---
            // --- 详细调试日志 ---
            // Log input values every frame when input is supposed to be enabled
            // 当输入应启用时，每帧记录输入值
            // Reduce frequency if too spammy (e.g., use Time.frameCount % 10 == 0)
            // 如果刷屏太严重，降低频率（例如，使用 Time.frameCount % 10 == 0）
            // Debug.Log($"[PlayerState Update] Input ACTIVE. Reading: x={xInput:F2}, y={yInput:F2}");
            // --- END DEBUG LOG ---
        }
        else
        {
            // If input is disabled, ensure input values are zero / 如果输入被禁用，确保输入值为零
            xInput = 0;
            yInput = 0;
            // Optional log when disabled (can be spammy) / 禁用时可选日志（可能刷屏）
            // if (player != null && Time.frameCount % 60 == 0) Debug.Log($"[PlayerState Update] Input DISABLED (isInputDisabled={player.IsInputDisabled}). x/y set to 0.");
        }
    }
}
