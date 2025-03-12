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

        Vector2 movementInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // 减少体力
        player.currentStamina -= player.depletionRate * Time.deltaTime;
        if (player.currentStamina <= 0)
        {
            player.currentStamina = 0;
            player.exhaustMarker = true;
            stateMachine.ChangeState(player.exhaustedState);
        }

        // 检查状态切换条件
        else if (movementInput == Vector2.zero)
        {
            stateMachine.ChangeState(player.idleState);
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(player.walkState);
        }
        else
        {
            player.SetVelocity(
                walkVelocity.x  * player.walkSpeed * player.accelerate, 
                walkVelocity.y * player.walkSpeed * player.accelerate
                );
        }
        
    }
}