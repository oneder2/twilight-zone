using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerExhaustedState : PlayerMoveState
{
    public PlayerExhaustedState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }
    
    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        
        // Walking in exhaused speed
        player.SetVelocity(
            walkVelocity.x  * player.walkSpeed * player.exhaustedFactor, 
            walkVelocity.y * player.walkSpeed * player.exhaustedFactor
            );
        
        // If not exhausted any more
        if (player.currentStamina == player.maxStamina)
        {
            // Switch to normal walk state
            stateMachine.ChangeState(player.walkState);
        }
    }
}
