using UnityEngine;
using UnityEngine.UI; // Assuming you still need Slider for stamina

public class Player : Singleton<Player> // Assuming Player doesn't inherit from Singleton<Player> anymore if placed in GameRoot
{
    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    #endregion

    #region State Machine
    // PlayerStateMachine might be better managed externally or passed in if Player is just data/logic
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerWalkState walkState { get; private set; }
    public PlayerRunState runState { get; private set; }
    public PlayerExhaustedState exhaustedState { get; private set; }
    public PlayerDeadState deadState { get; private set; }
    // Consider adding a PlayerCutsceneState if needed for specific cutscene behaviors
    #endregion

    #region Input Control
    // Flag to indicate if player input should be ignored (controlled by GameStatus)
    private bool isInputDisabled = false;
    // Public getter for PlayerState to check
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
    public Slider staminaBar; // Consider moving UI updates elsewhere (e.g., a dedicated PlayerUI script)
    #endregion

    [Header("Move info")]
    public float walkSpeed = 1;
    public float accelerate = 1.5f;

    // Removed static Instance if Player is now instantiated per session in GameRoot

    protected override void Awake()
    {
        // Get components
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Initialize State Machine
        // If PlayerStateMachine is a Singleton:
        stateMachine = PlayerStateMachine.Instance;
        // If PlayerStateMachine is a component on this GameObject:
        // stateMachine = GetComponent<PlayerStateMachine>();
        // if (stateMachine == null) stateMachine = gameObject.AddComponent<PlayerStateMachine>();

        // Initialize States
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        walkState = new PlayerWalkState(this, stateMachine, "Walk");
        exhaustedState = new PlayerExhaustedState(this, stateMachine, "Exhausted");
        runState = new PlayerRunState(this, stateMachine, "Run");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");
        // cutsceneState = new PlayerCutsceneState(this, stateMachine, "Idle"); // Example

        currentStamina = maxStamina;
    }

    // Subscribe to events when enabled
    private void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            Debug.Log("Player subscribed to GameStatusChangedEvent.");
        }
        // Immediately check current game state
        if (GameRunManager.Instance != null)
        {
            HandleGameStatusChange(new GameStatusChangedEvent(GameRunManager.Instance.CurrentStatus, GameStatus.Loading)); // Simulate event with current status
        }

        // REMOVE or REFACTOR the old Dialogue event listener:
        // EventManager.Instance.RemoveListener<DialogueStateChangedEvent>(OnDialogueStateChanged);
    }

    // Unsubscribe when disabled/destroyed
    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
            Debug.Log("Player unsubscribed from GameStatusChangedEvent.");
        }
        // Ensure input is re-enabled if disabled for other reasons? Or rely on GameStatus.Playing on re-enable.
        // isInputDisabled = false; // Maybe not needed if state is handled correctly on enable
    }


    private void Start()
    {
        // Initialize the state machine AFTER states are created in Awake
        if (stateMachine != null && idleState != null)
        {
             stateMachine.Initialize(idleState);
        } else {
             Debug.LogError("StateMachine or IdleState not initialized properly in Player.Awake!");
        }

        // Remove old dialogue listener logic from Start if it was here
    }

    private void Update()
    {
        // Update the current state of the state machine
        // The state itself will check IsInputDisabled before processing Input.GetAxisRaw
        stateMachine?.currentState?.Update();

        // Stamina regeneration logic (should probably only run when status is Playing)
        if (GameRunManager.Instance != null && GameRunManager.Instance.CurrentStatus == GameStatus.Playing)
        {
            UpdateStamina();
        }

        // Flip controller based on velocity (might want to disable this during cutscenes too?)
        if (!isInputDisabled) // Only flip based on movement if input is enabled
        {
             FlipController();
        }

        // Update Stamina UI (Consider moving this to a dedicated UI script)
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
    }

    /// <summary>
    /// Handles changes in the global game status.
    /// </summary>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        Debug.Log($"Player received GameStatusChangedEvent: {eventData.PreviousStatus} -> {eventData.NewStatus}");

        // Determine if input should be disabled based on the new state
        bool shouldDisableInput = true; // Default to disabled
        switch (eventData.NewStatus)
        {
            case GameStatus.Playing:
                shouldDisableInput = false; // Only enable input when actively playing
                break;
            case GameStatus.Paused:
            case GameStatus.InDialogue:
            case GameStatus.InCutscene:
            case GameStatus.Loading:
            case GameStatus.GameOver:
            case GameStatus.InMenu: // Should Player script even be active in Menu? Depends on setup.
                shouldDisableInput = true;
                break;
        }

        // Apply input disable state
        isInputDisabled = shouldDisableInput;
        Debug.Log($"Player Input Disabled: {isInputDisabled}");

        // If input is disabled, also force velocity to zero and potentially change state
        if (isInputDisabled)
        {
            ZeroVelocity();
            // Optional: Force state to Idle or a specific 'Inactive' state when input is disabled
            // This prevents states like Run from continuing without input.
            if (stateMachine?.currentState != idleState && stateMachine?.currentState != deadState) // Avoid interrupting dead state
            {
                 // Check if idleState is valid before changing
                 if (idleState != null) {
                      stateMachine.ChangeState(idleState);
                 } else {
                      Debug.LogError("Cannot change player state to Idle because idleState is null!");
                 }
            }
        }
    }


    private void UpdateStamina()
    {
        // When not running (and actively playing), regenerate stamina
        if (stateMachine.currentState != runState)
        {
            float currentRegenRate = IsMoving() ? regenerationRate : regenerationRate * fastRegenerationFactor;
            currentStamina += currentRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

            if (currentStamina == maxStamina)
            {
                exhaustMarker = false;
            }
        }
        // Note: Stamina depletion is handled within PlayerRunState's Update
    }


    // --- Utility Methods ---
    public void SetVelocity(float _xVelocity, float _yVelocity) { rb.linearVelocity = new Vector2(_xVelocity, _yVelocity); }
    public void ZeroVelocity() { if(rb != null) rb.linearVelocity = Vector2.zero; }
    public bool CanRun() { return currentStamina > 0 && !exhaustMarker; }
    public bool IsMoving() { return rb != null && rb.linearVelocity != Vector2.zero; }
    public void DisableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = false; }
    public void EnableCollision() { if(TryGetComponent<Collider2D>(out var col)) col.enabled = true; }

    private void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        // Assuming sprite is on a child object
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.flipX = !renderer.flipX;
    }

    private void FlipController()
    {
        // Only flip if moving horizontally and input is not disabled
        if (rb == null || isInputDisabled) return;

        if (rb.linearVelocity.x > 0.1f && !facingRight) // Use a small threshold
            Flip();
        else if (rb.linearVelocity.x < -0.1f && facingRight)
            Flip();
    }
}