using UnityEngine;

/// <summary>
/// Example script demonstrating how to start the game session.
/// Attach this to a GameObject in your MainMenu scene.
/// </summary>
public class GameStartHandler : MonoBehaviour
{
    public void StartGame()
    {
        EventManager.Instance.TriggerEvent(new GameStartEvent());
    }
}