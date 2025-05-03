// File: Scripts/Player/Player.cs
using UnityEngine;
using UnityEngine.UI; // Or remove if Slider is not used / 如果未使用 Slider 则移除

/// <summary>
/// Main Player class, handling components, state machine, stamina, movement, and game status interaction.
/// 玩家主类，处理组件、状态机、体力、移动以及与游戏状态的交互。
/// </summary>
public class Player : Singleton<Player>
{
    #region Components / 组件
    public Animator anim { get; private set; } // Animator component / Animator 组件
    public Rigidbody2D rb { get; private set; } // Rigidbody2D component / Rigidbody2D 组件
    #endregion

    #region State Machine / 状态机
    public PlayerStateMachine stateMachine { get; private set; } // Reference to the state machine / 对状态机的引用
    // State instances / 状态实例
    public PlayerIdleState idleState { get; private set; }
    public PlayerWalkState walkState { get; private set; }
    public PlayerRunState runState { get; private set; }
    public PlayerExhaustedState exhaustedState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    #region Input Control / 输入控制
    private bool isInputDisabled = false; // Flag to disable player input / 禁用玩家输入的标志
    public bool IsInputDisabled => isInputDisabled; // Public accessor / 公共访问器
    #endregion

    #region Flipping / 翻转
    private bool facingRight = true; // Is the player facing right? / 玩家是否朝右？
    private int facingDir = 1; // Direction multiplier (1 for right, -1 for left) / 方向乘数（右为 1，左为 -1）
    #endregion

    #region Stamina / 体力
    public float maxStamina = 100f; // Maximum stamina value / 最大体力值
    public float currentStamina; // Current stamina value / 当前体力值
    public float regenerationRate = 10f; // Stamina regeneration rate per second / 每秒体力恢复速率
    public float fastRegenerationFactor = 3; // Multiplier for faster regen when idle / 静止时快速恢复的乘数
    public float depletionRate = 20f; // Stamina depletion rate per second when running / 跑步时每秒体力消耗速率
    public bool exhaustMarker = false; // Flag indicating player is exhausted / 表示玩家已力竭的标志
    public float exhaustedFactor = 0.8f; // Speed multiplier when exhausted / 力竭时的速度乘数
    public Slider staminaBar; // UI Slider for stamina display / 用于显示体力的 UI Slider
    #endregion

    [Header("Move info / 移动信息")]
    public float walkSpeed = 1; // Base walking speed / 基本行走速度
    public float accelerate = 1.5f; // Speed multiplier when running / 跑步时的速度乘数

    private bool isStateMachineInitialized = false; // Flag to track state machine initialization / 跟踪状态机初始化的标志

    // Awake is called when the script instance is being loaded
    // Awake 在加载脚本实例时被调用
    protected override void Awake()
    {
        base.Awake(); // Handle Singleton logic / 处理 Singleton 逻辑

        // Get components / 获取组件
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Initialize State Machine Instance / 初始化状态机实例
        stateMachine = PlayerStateMachine.Instance; // Assuming PlayerStateMachine is a Singleton / 假设 PlayerStateMachine 是一个单例

        // Initialize States / 初始化状态
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        walkState = new PlayerWalkState(this, stateMachine, "Walk");
        exhaustedState = new PlayerExhaustedState(this, stateMachine, "Exhausted");
        runState = new PlayerRunState(this, stateMachine, "Run");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

        // Initialize stamina / 初始化体力
        currentStamina = maxStamina;

        // Initialize the state machine AFTER states are created / 在创建状态后初始化状态机
        if (stateMachine != null && idleState != null)
        {
             stateMachine.Initialize(idleState); // Start in Idle state / 从 Idle 状态开始
             isStateMachineInitialized = true;
             // Debug.Log("[Player.Awake] State machine initialized.");
        } else {
             Debug.LogError("[Player.Awake] StateMachine or IdleState is null during Awake initialization!");
        }
    }

    // OnEnable is called when the object becomes enabled and active
    // OnEnable 在对象变为启用和活动状态时被调用
    private void OnEnable()
    {
        // Subscribe to game status changes / 订阅游戏状态更改
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            // Debug.Log("[Player.OnEnable] Subscribed to GameStatusChangedEvent.");
        }
        else
        {
             Debug.LogWarning("[Player.OnEnable] EventManager not found, cannot subscribe.");
        }

        // Apply initial game status effects if ready / 如果准备就绪，则应用初始游戏状态效果
        if (isStateMachineInitialized && GameRunManager.Instance != null)
        {
            // Simulate the event to apply initial status correctly / 模拟事件以正确应用初始状态
            HandleGameStatusChange(new GameStatusChangedEvent(GameRunManager.Instance.CurrentStatus, GameStatus.Loading)); // Assume previous was Loading / 假设之前是 Loading
        }
        else if (!isStateMachineInitialized)
        {
             // Debug.LogWarning("[Player.OnEnable] State machine not initialized yet. Initial status check deferred.");
        }
    }

    // OnDisable is called when the object becomes disabled or inactive
    // OnDisable 在对象变为禁用或非活动状态时被调用
    private void OnDisable()
    {
        // Unsubscribe from events / 取消订阅事件
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
        }
    }

    // Update is called once per frame
    // Update 每帧调用一次
    private void Update()
    {
        // --- DEBUG LOG: Check if Player Update is running ---
        // --- 调试日志：检查 Player Update 是否正在运行 ---
        // if (Time.frameCount % 120 == 0) // Print roughly every 2 seconds / 大约每 2 秒打印一次
        // {
        //      Debug.Log($"[Player Update Check] Player GO Active: {gameObject.activeInHierarchy}, Script Enabled: {this.enabled}, Current State: {stateMachine?.currentState?.GetType().Name ?? "N/A"}, isInputDisabled: {isInputDisabled}");
        // }
        // --- END DEBUG LOG ---

        // Ensure state machine is ready before updating / 在更新之前确保状态机已准备就绪
        if (!isStateMachineInitialized) return;

        // Update the current state of the state machine / 更新状态机的当前状态
        stateMachine?.currentState?.Update(); // Use null-conditional operator ?. for safety / 使用空条件运算符 ?. 以确保安全

        // Stamina regeneration logic (only when playing) / 体力恢复逻辑（仅在游戏中）
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            UpdateStamina();
        }

        // Flip controller (only if input is enabled) / 翻转控制器（仅当输入启用时）
        if (!isInputDisabled)
        {
             FlipController();
        }

        // Update Stamina UI / 更新体力 UI
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
    }

    /// <summary>
    /// Handles changes in the global game status, enabling/disabling input and forcing Idle state.
    /// 处理全局游戏状态的变化，启用/禁用输入并强制进入 Idle 状态。
    /// </summary>
    /// <param name="eventData">Event data containing new and previous status. / 包含新旧状态的事件数据。</param>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        // Ensure the state machine is initialized before proceeding / 在继续之前确保状态机已初始化
        if (!isStateMachineInitialized)
        {
            // Debug.LogWarning($"[Player.HandleGameStatusChange] Received event {eventData.NewStatus} but state machine not initialized yet.");
            // Determine input disable state early without changing state machine / 在不更改状态机的情况下尽早确定输入禁用状态
            bool shouldDisableInputEarly = eventData.NewStatus != GameStatus.Playing;
             isInputDisabled = shouldDisableInputEarly;
             if(isInputDisabled) ZeroVelocity(); // Still zero velocity if input disabled / 如果输入禁用，仍然将速度归零
            return; // Exit early / 提前退出
        }

        GameStatus newStatus = eventData.NewStatus;
        bool shouldDisableInput; // Removed default assignment / 移除了默认赋值

        // Determine if input should be disabled based on the new state / 根据新状态确定是否应禁用输入
        // Debug.LogError($"从旧状态切换到了新的状态：{newStatus}");
        switch (newStatus)
        {
            case GameStatus.Playing:
                shouldDisableInput = false; // Enable input for Playing state / 为 Playing 状态启用输入
                break;
            // All other states disable input / 所有其他状态禁用输入
            case GameStatus.Paused:
            case GameStatus.InDialogue: // Input should be disabled during blocking dialogue / 阻塞式对话期间应禁用输入
            case GameStatus.InCutscene:
            case GameStatus.Loading:
            case GameStatus.GameOver:
            case GameStatus.InMenu:
            default: // Default case to ensure it's always assigned / 默认情况以确保始终赋值
                shouldDisableInput = true;
                break;
        }

        // --- CRITICAL: Apply the calculated value ---
        // --- 关键：应用计算出的值 ---
        isInputDisabled = shouldDisableInput;

        // --- DEBUG LOG: Confirm isInputDisabled value ---
        // --- 调试日志：确认 isInputDisabled 的值 ---
        Debug.Log($"[Player HandleGameStatusChange] Status changed to {newStatus}. Setting isInputDisabled = {isInputDisabled}");
        // --- END DEBUG LOG ---

        // If input is disabled, stop movement and force Idle state (unless Dead)
        // 如果输入被禁用，则停止移动并强制进入 Idle 状态（除非 Dead）
        if (isInputDisabled)
        {
            ZeroVelocity(); // Stop movement / 停止移动

            // Check state machine and states before changing / 更改前检查状态机和状态
            if (stateMachine != null && stateMachine.currentState != null && idleState != null && stateMachine.currentState != deadState)
            {
                 // Only force Idle if not already Idle / 仅当尚未处于 Idle 状态时才强制 Idle
                 if (stateMachine.currentState != idleState)
                 {
                      // Debug.Log("[Player HandleGameStatusChange] Input disabled, changing state to Idle.");
                      stateMachine.ChangeState(idleState); // Force Idle / 强制 Idle
                 }
            }
        }
        // No 'else' needed: When input becomes enabled (isInputDisabled = false),
        // PlayerState.Update() will read input, and the state machine will transition naturally.
        // 不需要 'else'：当输入启用时 (isInputDisabled = false)，PlayerState.Update() 将读取输入，
        // 状态机将根据该输入自然转换。
    }

    /// <summary>
    /// Updates the player's current stamina based on regeneration rules.
    /// 根据恢复规则更新玩家的当前体力。
    /// </summary>
    private void UpdateStamina()
    {
        if (!isStateMachineInitialized) return; // Ensure ready / 确保准备就绪

        // Don't regenerate stamina while running / 跑步时不恢复体力
        if (stateMachine?.currentState == runState) return;

        // Regenerate faster when idle / 静止时恢复更快
        float currentRegenRate = IsMoving() ? regenerationRate : regenerationRate * fastRegenerationFactor;
        currentStamina += currentRegenRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina); // Clamp between 0 and max / 限制在 0 和最大值之间

        // Reset exhausted marker when fully recovered / 完全恢复时重置力竭标记
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina; // Ensure it doesn't exceed max / 确保不超过最大值
            exhaustMarker = false;
        }
    }

    #region Utility Methods / 实用方法
    /// <summary> Sets the Rigidbody's linear velocity. / 设置 Rigidbody 的线性速度。 </summary>
    public void SetVelocity(float _xVelocity, float _yVelocity) { if(rb != null) rb.linearVelocity = new Vector2(_xVelocity, _yVelocity); }
    /// <summary> Sets the Rigidbody's linear velocity to zero. / 将 Rigidbody 的线性速度设置为零。 </summary>
    public void ZeroVelocity() { if(rb != null) rb.linearVelocity = Vector2.zero; }
    /// <summary> Checks if the player can currently run (has stamina, not exhausted). / 检查玩家当前是否可以跑步（有体力，未力竭）。 </summary>
    public bool CanRun() { return currentStamina > 0 && !exhaustMarker; }
    /// <summary> Checks if the player is currently moving significantly. / 检查玩家当前是否有明显移动。 </summary>
    public bool IsMoving() { return rb != null && rb.linearVelocity.sqrMagnitude > 0.01f; } // Use sqrMagnitude for efficiency / 使用 sqrMagnitude 以提高效率
    /// <summary> Disables the player's 2D collider. / 禁用玩家的 2D 碰撞器。 </summary>
    public void DisableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = false; }
    /// <summary> Enables the player's 2D collider. / 启用玩家的 2D 碰撞器。 </summary>
    public void EnableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = true; }

    /// <summary> Flips the player sprite horizontally. / 水平翻转玩家精灵图。 </summary>
    private void Flip()
    {
        facingDir *= -1; // Invert direction multiplier / 反转方向乘数
        facingRight = !facingRight; // Toggle facing flag / 切换朝向标志
        // Assuming SpriteRenderer is on a child object or the same object
        // 假设 SpriteRenderer 在子对象或同一对象上
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.flipX = !facingRight; // Set flipX based on facing direction / 根据朝向设置 flipX
        }
    }

    /// <summary> Controls sprite flipping based on horizontal input. / 根据水平输入控制精灵图翻转。 </summary>
    private void FlipController()
    {
        if (rb == null || isInputDisabled) return; // Don't flip if input disabled / 如果输入禁用则不翻转

        // Read horizontal input directly for immediate response / 直接读取水平输入以获得即时响应
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // Flip if input direction opposes current facing direction / 如果输入方向与当前朝向相反则翻转
        if (horizontalInput > 0.1f && !facingRight)
            Flip();
        else if (horizontalInput < -0.1f && facingRight)
            Flip();
    }

    /// <summary>
    /// Resets the player's state to default values, typically used on game start/restart.
    /// 将玩家状态重置为默认值，通常在游戏开始/重新开始时使用。
    /// </summary>
    /// <param name="spawnPosition">The position where the player should spawn. / 玩家应该生成的位置。</param>
    public void ResetPlayerState(Vector3 spawnPosition)
    {
        if (!isStateMachineInitialized) { Debug.LogError("[Player.ResetPlayerState] State machine not initialized!"); return; }

        // 1. Reset Position / 1. 重置位置
        transform.position = spawnPosition;

        // 2. Reset Physics State / 2. 重置物理状态
        ZeroVelocity(); // Stop any movement / 停止任何移动
        if (rb != null) { rb.angularVelocity = 0f; } // Reset rotation speed / 重置旋转速度

        // 3. Reset Stamina / 3. 重置体力
        currentStamina = maxStamina;
        exhaustMarker = false;
        if (staminaBar != null) staminaBar.value = 1f; // Update UI / 更新 UI

        // 4. Reset Collision State (Ensure enabled) / 4. 重置碰撞状态（确保已启用）
        EnableCollision();

        // 5. Input Flag will be handled by GameRunManager based on Playing state / 输入标志将由 GameRunManager 根据 Playing 状态处理

        // 6. Reset State Machine to Idle State / 6. 将状态机重置为 Idle 状态
        if (stateMachine != null && idleState != null)
        {
            stateMachine.Initialize(idleState); // Re-initialize to safely set Idle / 重新初始化以安全设置 Idle
            // Debug.Log("[Player] State machine re-initialized to Idle state after reset.");
        } else { Debug.LogWarning("[Player] StateMachine or IdleState is null during reset."); }

         // 7. Ensure correct facing direction (default to right) / 7. 确保正确的朝向（默认为右）
         if (!facingRight) { Flip(); }
    }
    #endregion

} // End of Player class / Player 类结束
