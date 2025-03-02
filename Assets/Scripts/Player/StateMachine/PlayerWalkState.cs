using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerMoveState
{
    public PlayerWalkState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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

        player.SetVelocity(walkVelocity.x  * player.walkSpeed, walkVelocity.y * player.walkSpeed);

        if (Input.GetKey(KeyCode.LeftShift))
            stateMachine.ChangeState(player.runState);
    }
}
