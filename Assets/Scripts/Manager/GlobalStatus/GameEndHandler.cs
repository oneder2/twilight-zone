using UnityEngine;

/// <summary>
/// Example script demonstrating how to start the game session.
/// Attach this to a GameObject in your MainMenu scene.
/// </summary>
public class GameEndHandler : MonoBehaviour
{
    public void EndGame()
    {
        EventManager.Instance.TriggerEvent(new GameEndEvent());
    }
}