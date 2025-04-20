using UnityEngine;

public class PlayerState
{
    protected PlayerStateMachine stateMachine;
    protected Player player; // Reference to the main Player script
    protected string animBoolName;

    protected float xInput;
    protected float yInput;

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Enter()
    {
        // Ensure player and anim are valid
        if (player?.anim != null)
        {
             player.anim.SetBool(animBoolName, true);
        } else {
             Debug.LogError($"Player or Animator is null in PlayerState.Enter for state: {this.GetType().Name}");
        }
    }

    public virtual void Exit()
    {
        if (player?.anim != null)
        {
             player.anim.SetBool(animBoolName, false);
        }
    }

    public virtual void Update()
    {
        // --- Input Reading Modification ---
        // Only read input if the Player script allows it (based on GameStatus)
        if (player != null && !player.IsInputDisabled)
        {
            xInput = Input.GetAxisRaw("Horizontal");
            yInput = Input.GetAxisRaw("Vertical");
            // If using Unity's new Input System, read values here instead
        }
        else
        {
            // If input is disabled, ensure input values are zero
            xInput = 0;
            yInput = 0;
        }
        // --- End Input Reading Modification ---

        // Other general state update logic can go here
    }
}