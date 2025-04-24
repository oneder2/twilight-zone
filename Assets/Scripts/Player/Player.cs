// File: Scripts/Player/Player.cs
using UnityEngine;
using UnityEngine.UI; // Assuming you still need Slider for stamina // 假设你仍然需要 Slider 来显示体力

public class Player : Singleton<Player>
{
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    #endregion

    #region State Machine
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerWalkState walkState { get; private set; }
    public PlayerRunState runState { get; private set; }
    public PlayerExhaustedState exhaustedState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    #endregion

    #region Input Control
    private bool isInputDisabled = false;
    public bool IsInputDisabled => isInputDisabled;
    #endregion

    #region Flipping
    private bool facingRight = true;
    private int facingDir = 1;
    #endregion

    #region Stamina
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenerationRate = 10f;
    public float fastRegenerationFactor = 3;
    public float depletionRate = 20f;
    public bool exhaustMarker = false;
    public float exhaustedFactor = 0.8f;
    public Slider staminaBar;
    #endregion

    [Header("Move info")]
    public float walkSpeed = 1;
    public float accelerate = 1.5f;

    private bool isStateMachineInitialized = false; // Add a flag // 添加一个标志

    protected override void Awake()
    {
        // Get components
        // 获取组件
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Initialize State Machine Instance
        // 初始化状态机实例
        stateMachine = PlayerStateMachine.Instance; // Assuming PlayerStateMachine is a Singleton // 假设 PlayerStateMachine 是一个单例

        // Initialize States
        // 初始化状态
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        walkState = new PlayerWalkState(this, stateMachine, "Walk");
        exhaustedState = new PlayerExhaustedState(this, stateMachine, "Exhausted");
        runState = new PlayerRunState(this, stateMachine, "Run");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

        currentStamina = maxStamina;

        // Initialize the state machine AFTER states are created, right here in Awake
        // 在 Awake 中创建状态后立即初始化状态机
        if (stateMachine != null && idleState != null)
        {
             stateMachine.Initialize(idleState);
             isStateMachineInitialized = true; // Set the flag // 设置标志
             Debug.Log("[Player.Awake] State machine initialized.");
        } else {
             Debug.LogError("[Player.Awake] StateMachine or IdleState is null during Awake initialization!");
        }
    }

    // Subscribe to events when enabled
    // 启用时订阅事件
    private void OnEnable()
    {
        // Subscribe first
        // 先订阅
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            Debug.Log("[Player.OnEnable] Subscribed to GameStatusChangedEvent.");
        }
        else
        {
             Debug.LogWarning("[Player.OnEnable] EventManager not found, cannot subscribe.");
        }

        // Then, check initial state *if* the state machine is ready
        // 然后，*如果*状态机准备就绪，检查初始状态
        if (isStateMachineInitialized && GameRunManager.Instance != null)
        {
            // Simulate the event to apply initial status effects correctly
            // 模拟事件以正确应用初始状态效果
            HandleGameStatusChange(new GameStatusChangedEvent(GameRunManager.Instance.CurrentStatus, GameStatus.Loading));
        }
        else if (!isStateMachineInitialized)
        {
             Debug.LogWarning("[Player.OnEnable] State machine not initialized yet. Initial status check deferred.");
             // The first GameStatusChangedEvent received *after* initialization will handle it.
             // 初始化*之后*收到的第一个 GameStatusChangedEvent 将处理它。
        }

    }

    // Unsubscribe when disabled/destroyed
    // 禁用/销毁时取消订阅
    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
            // Debug.Log("Player unsubscribed from GameStatusChangedEvent."); // Less verbose logging // 减少冗余日志
        }
    }

    private void Update()
    {
        // Ensure state machine is ready before updating
        // 在更新之前确保状态机已准备就绪
        if (!isStateMachineInitialized) return;

        // Update the current state of the state machine
        // 更新状态机的当前状态
        stateMachine?.currentState?.Update(); // Use null-conditional operator ?. for safety // 使用空条件运算符 ?. 以确保安全

        // Stamina regeneration logic
        // 体力恢复逻辑
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            UpdateStamina();
        }

        // Flip controller
        // 翻转控制器
        if (!isInputDisabled)
        {
             FlipController();
        }

        // Update Stamina UI
        // 更新体力 UI
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
    }

    /// <summary>
    /// Handles changes in the global game status.
    /// 处理全局游戏状态的变化。
    /// </summary>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        // --- MODIFICATION START ---
        // --- 修改开始 ---
        // Ensure the state machine is initialized before proceeding
        // 在继续之前确保状态机已初始化
        if (!isStateMachineInitialized)
        {
            Debug.LogWarning($"[Player.HandleGameStatusChange] Received event {eventData.NewStatus} but state machine not initialized yet. Ignoring state change attempt.");
            // Determine input disable state based on event *without* changing state machine state
            // 根据事件确定输入禁用状态，*不*更改状态机状态
            bool shouldDisableInputEarly = true;
             switch (eventData.NewStatus)
             {
                 case GameStatus.Playing: shouldDisableInputEarly = false; break;
                 default: shouldDisableInputEarly = true; break;
             }
             isInputDisabled = shouldDisableInputEarly;
             if(isInputDisabled) ZeroVelocity(); // Still zero velocity if input disabled // 如果输入禁用，仍然将速度归零
            return; // Exit early // 提前退出
        }
        // --- MODIFICATION END ---


        // Debug.Log($"Player received GameStatusChangedEvent: {eventData.PreviousStatus} -> {eventData.NewStatus}"); // Less verbose // 减少冗余

        // Determine if input should be disabled based on the new state
        // 根据新状态确定是否应禁用输入
        bool shouldDisableInput = true;
        switch (eventData.NewStatus)
        {
            case GameStatus.Playing:
                shouldDisableInput = false;
                break;
            case GameStatus.Paused:
            case GameStatus.InDialogue:
            case GameStatus.InCutscene:
            case GameStatus.Loading:
            case GameStatus.GameOver:
            case GameStatus.InMenu:
                shouldDisableInput = true;
                break;
        }

        isInputDisabled = shouldDisableInput;
        // Debug.Log($"Player Input Disabled: {isInputDisabled}"); // Less verbose // 减少冗余

        if (isInputDisabled)
        {
            ZeroVelocity();

            // Now that we know isStateMachineInitialized is true, these checks are safer
            // 既然我们知道 isStateMachineInitialized 为 true，这些检查更安全
            if (stateMachine != null && stateMachine.currentState != null && idleState != null)
            {
                 // Only change state if not already Idle or Dead
                 // 仅当状态不是 Idle 或 Dead 时才更改状态
                 if (stateMachine.currentState != idleState && stateMachine.currentState != deadState)
                 {
                      stateMachine.ChangeState(idleState);
                 }
            }
            // No need for the warning here anymore because of the initial check
            // 由于初始检查，此处不再需要警告
        }
    }


    private void UpdateStamina()
    {
        // Ensure state machine is initialized
        // 确保状态机已初始化
        if (!isStateMachineInitialized) return;

        if (stateMachine?.currentState != runState) // Null check for safety // 安全性空值检查
        {
            float currentRegenRate = IsMoving() ? regenerationRate : regenerationRate * fastRegenerationFactor;
            currentStamina += currentRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

            if (currentStamina >= maxStamina) // Use >= for robustness // 使用 >= 增加健壮性
            {
                currentStamina = maxStamina; // Ensure it doesn't exceed max // 确保不超过最大值
                exhaustMarker = false;
            }
        }
    }


    // --- Utility Methods ---
    // --- 实用方法 ---
    public void SetVelocity(float _xVelocity, float _yVelocity) { if(rb != null) rb.linearVelocity = new Vector2(_xVelocity, _yVelocity); }
    public void ZeroVelocity() { if(rb != null) rb.linearVelocity = Vector2.zero; }
    public bool CanRun() { return currentStamina > 0 && !exhaustMarker; }
    public bool IsMoving() { return rb != null && rb.linearVelocity.sqrMagnitude > 0.01f; }
    public void DisableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = false; }
    public void EnableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = true; }

    private void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.flipX = !facingRight;
        }
    }

    private void FlipController()
    {
        if (rb == null || isInputDisabled) return;
        float horizontalVelocity = rb.linearVelocityX;
        if (horizontalVelocity > 0.1f && !facingRight)
            Flip();
        else if (horizontalVelocity < -0.1f && facingRight)
            Flip();
    }


    #region Reset Logic (Refactored)
    /// <summary>
    /// Resets the player's state to default values, typically used on game start/restart.
    /// 将玩家状态重置为默认值，通常在游戏开始/重新开始时使用。
    /// This method now also ensures the state machine is correctly set to Idle.
    /// 此方法现在还确保状态机正确设置为 Idle。
    /// </summary>
    /// <param name="spawnPosition">The position where the player should spawn. / 玩家应该生成的位置。</param>
    public void ResetPlayerState(Vector3 spawnPosition)
    {
        Debug.Log($"[Player] Resetting state to spawn at {spawnPosition}");

         // Ensure state machine is initialized before resetting state that depends on it
         // 在重置依赖于状态机的状态之前，确保状态机已初始化
        if (!isStateMachineInitialized)
        {
             Debug.LogError("[Player.ResetPlayerState] Cannot reset state, state machine is not initialized!");
             return;
        }

        // 1. Reset Position
        // 1. 重置位置
        transform.position = spawnPosition;

        // 2. Reset Physics State
        // 2. 重置物理状态
        ZeroVelocity(); // Stop any movement // 停止任何移动
        if (rb != null)
        {
            rb.angularVelocity = 0f; // Reset rotation speed // 重置旋转速度
        }

        // 3. Reset Stamina
        // 3. 重置体力
        currentStamina = maxStamina;
        exhaustMarker = false;
        if (staminaBar != null) staminaBar.value = 1f; // Update UI // 更新 UI

        // 4. Reset Collision State (Ensure enabled)
        // 4. 重置碰撞状态（确保已启用）
        EnableCollision();

        // 5. Reset Input Flag (GameRunManager will handle based on Playing state)
        // 5. 重置输入标志（GameRunManager 将根据 Playing 状态处理）
        // isInputDisabled = false; // Let GameRunManager handle this // 让 GameRunManager 处理这个

        // 6. Reset any other game-specific player stats if needed
        // 6. 如果需要，重置任何其他特定于游戏的玩家统计数据
        // Example: if (Inventory.Instance != null) Inventory.Instance.Clear(); // Careful! // 小心！

        // 7. Reset State Machine to Idle State
        // 7. 将状态机重置为 Idle 状态
        if (stateMachine != null && idleState != null)
        {
            // Re-initialize to safely set Idle as current and call Enter() correctly
            // 重新初始化以安全地将 Idle 设置为当前状态并正确调用 Enter()
            stateMachine.Initialize(idleState);
             Debug.Log("[Player] State machine re-initialized to Idle state after reset.");
        }
         else { Debug.LogWarning("[Player] StateMachine or IdleState is null during reset. Cannot re-initialize state machine."); }

         // 8. Ensure correct facing direction (optional, default to right)
         // 8. 确保正确的朝向（可选，默认为右）
         if (!facingRight) {
              Flip(); // Flip to face right if currently facing left // 如果当前朝左，则翻转以朝右
         }
    }
    #endregion

} // End of Player class // Player 类结束
