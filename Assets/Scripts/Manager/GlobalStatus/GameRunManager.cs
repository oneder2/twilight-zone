using UnityEngine;
using UnityEngine.SceneManagement; // Reference the Scene Management namespace


// Recommended: More detailed game status enum
public enum GameStatus { InMenu, Loading, Playing, Paused, GameOver }




/// <summary>
/// Manages the global game running state, distinguishing between the main menu and an active game session.
/// Also provides functionality to pause and resume the game.
/// This script should be placed in the GameRoot scene as a game-session-level manager.
/// </summary>
public class GameRunManager : Singleton<GameRunManager> // Inherit from your project's Singleton base class
{
    // --- Using your defined simple state ---
    /// <summary>
    /// Is the game currently in an active session (true) or in the menu/loading (false)?
    /// </summary>
    public bool IsInGameSession { get; private set; } = false; // Default to not in game

    // --- Recommended: Using more detailed state ---
    /// <summary>
    /// The current detailed status of the game (e.g., Playing, Paused, GameOver).
    /// </summary>
    public GameStatus CurrentStatus { get; private set; } = GameStatus.Loading; // Initial state can be Loading or InMenu

    /// <summary>
    /// Is the game currently paused?
    /// </summary>
    public bool IsPaused => CurrentStatus == GameStatus.Paused;

    // Unity Lifecycle Method: Called when the GameRoot scene loads and this script initializes
    void Start()
    {
        // When GameRunManager starts, it signifies the beginning of a game session

        // Recommended: Also update the detailed status
        ChangeGameStatus(GameStatus.InMenu); // Assume it transitions to Playing state after loading
    }

    /// <summary>
    /// Changes the detailed game status (CurrentStatus) and triggers the corresponding event.
    /// This is the recommended way to manage state.
    /// </summary>
    /// <param name="newStatus">The new game status.</param>
    public void ChangeGameStatus(GameStatus newStatus)
    {
        if (CurrentStatus == newStatus) return; // State hasn't changed

        GameStatus previousStatus = CurrentStatus;
        CurrentStatus = newStatus;
        Debug.Log($"[GameRunManager] Game Status Changed: {previousStatus} -> {CurrentStatus}");

        // Handle logic for entering/exiting states (e.g., pausing/resuming)
        HandleStatusChange(previousStatus, newStatus);

        // Trigger the more detailed status change event (recommended to use this event)
        EventManager.Instance.TriggerEvent(new GameStatusChangedEvent(CurrentStatus, previousStatus));
    }

    /// <summary>
    /// Executes specific actions based on the status change (e.g., setting Time.timeScale).
    /// </summary>
    private void HandleStatusChange(GameStatus oldStatus, GameStatus newStatus)
    {
        // Pause logic
        if (newStatus == GameStatus.Paused)
        {
            Time.timeScale = 0f;
            // Could trigger a more specific PauseEvent here for other systems (Input, AI) to react
            // EventManager.Instance.TriggerEvent(new GamePausedEvent());
        }
        // Resume logic (when coming from a Paused state)
        else if (oldStatus == GameStatus.Paused && newStatus != GameStatus.Paused)
        {
            Time.timeScale = 1f;
            // Could trigger a more specific ResumeEvent here
            // EventManager.Instance.TriggerEvent(new GameResumedEvent());
        }

        // Other state handling logic...
        // For example, entering GameOver state might disable player input, show end screen, etc.
        if (newStatus == GameStatus.GameOver)
        {
            // ... Handle game over logic ...
        }

        // Handle entering menu state (usually happens after EndGameSession)
        if (newStatus == GameStatus.InMenu)
        {
             Time.timeScale = 1f; // Ensure time is normal in the menu
        }
    }


    /// <summary>
    /// Starts a new game session.
    /// This method should be called *Before* loading process to return to the main menu.
    /// </summary>
    public void StartGameSession()
    {
        // Info Log: Start a new game session
        Debug.Log("[GameRunManager] Starting Game Session...");

        // Change the Game status to playing
        ChangeGameStatus(GameStatus.Playing);
    }


    /// <summary>
    /// Pauses the game.
    /// External systems (like PauseMenu) should call this method.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentStatus == GameStatus.Playing) // Can only pause if currently playing
        {
            ChangeGameStatus(GameStatus.Paused);
        }
    }

    /// <summary>
    /// Resumes the game.
    /// External systems (like PauseMenu) should call this method.
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentStatus == GameStatus.Paused) // Can only resume from Paused state
        {
            ChangeGameStatus(GameStatus.Playing);
        }
    }

    /// <summary>
    /// Ends the current game session.
    /// This method should be called *before* initiating the scene unloading process to return to the main menu.
    /// </summary>
    public void EndGameSession()
    {
        Debug.Log("[GameRunManager] Ending Game Session...");

        // 2. Update the detailed status to InMenu or Loading (depending on your flow)
        ChangeGameStatus(GameStatus.InMenu); // Or GameStatus.Loading if the menu needs loading time

        // 3. Any cleanup work that needs to happen before GameRoot is unloaded can be added here
        EventManager.Instance.RemoveAllListenersForType<StageChangeEvent>();

        // 4. Next steps (executed in the external code that calls this method):
        Scene currentLevelScene = SceneManager.GetActiveScene();
        SceneManager.UnloadSceneAsync("GameRoot");
        SceneManager.UnloadSceneAsync(currentLevelScene);
        SceneManager.LoadSceneAsync("MainMenuScene");
    }

    // Optional: Perform final checks or cleanup in OnDestroy, but be cautious as other objects might already be destroyed
    // void OnDestroy()
    // {
    //     // Ensure time scale is reset, just in case
    //     Time.timeScale = 1f;
    //     // If EventManager is application-level and needs it, unsubscribe from events here
    // }
}
