using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 示例状态：Idle
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public override void Enter()
    {
        base.Enter();
        player.ZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        if (xInput != 0 || yInput != 0)
        {
            if (player.exhaustMarker)
            {
                stateMachine.ChangeState(player.exhaustedState);
            }
            stateMachine.ChangeState(player.walkState);
        }
    }
}