using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 示例状态：Idle
public class PlayerMoveState : PlayerState
{
    protected Vector2 walkVelocity;
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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

        walkVelocity.x = xInput;
        walkVelocity.y = yInput;
        walkVelocity = walkVelocity.normalized;

        if (xInput == 0 && yInput ==0)
            stateMachine.ChangeState(player.idleState);
    }
}