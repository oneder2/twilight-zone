using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerStateMachine : Singleton<PlayerStateMachine>
{
    public PlayerState currentState {get; private set;}

    public void Initialize(PlayerState _startState)
    {
        currentState = _startState;
        currentState.Enter();
        Debug.Log($"CurrentState是：：：：：{currentState}");
    }

    public void ChangeState(PlayerState _newState)
    {
        Debug.Log($"CurrentState是：：：：：{currentState}");
        currentState.Exit();
        currentState = _newState;
        currentState.Enter();
    }

}
