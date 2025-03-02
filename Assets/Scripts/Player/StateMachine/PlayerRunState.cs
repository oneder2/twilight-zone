using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 示例状态：Idle
public class PlayerRunState : PlayerMoveState
{
    public PlayerRunState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
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

        player.SetVelocity(walkVelocity.x  * player.walkSpeed * player.accelerate, walkVelocity.y * player.walkSpeed * player.accelerate);

        if (Input.GetKeyUp(KeyCode.LeftShift))
            stateMachine.ChangeState(player.walkState);
    }
}