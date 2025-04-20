using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement; // Reference the Scene Management namespace


// Recommended: More detailed game status enum
public enum GameStatus { 
    InMenu, 
    Loading, 
    Playing, 
    Paused, 
    InCutscene, 
    GameOver, 
    InDialogue 
    }

// Assuming definitions for GameStatus, GameStatusChangedEvent, CutscenePlayer, SessionStateManager, EventManager exist
// Assuming relevant Event classes like BeforeSceneUnloadEvent, AfterSceneUnloadEvent exist

/// <summary>
/// Manages the overall game running state (Playing, Paused, InCutscene, etc.)
/// and handles global cutscene management like skipping, tracking the active player,
/// and initiating cutscenes requested by triggers.
/// Should be a Singleton placed in an early Boot scene and marked DontDestroyOnLoad.
/// </summary>
public class GameRunManager : Singleton<GameRunManager> // Use your Singleton base class
{
    [Header("State Management")]
    /// <summary>
    /// The current detailed status of the game.
    /// </summary>
    public GameStatus CurrentStatus { get; private set; } = GameStatus.InMenu; // Start in Menu

    /// <summary>
    /// Is the game currently paused?
    /// </summary>
    public bool IsPaused => CurrentStatus == GameStatus.Paused;

    [Header("Cutscene Management")]
    [Tooltip("Reference to the CutscenePlayer instance (usually in the GameRoot scene). Assign via Inspector in the Boot scene IF CutscenePlayer also persists, otherwise find dynamically or use registration.")]
    // IMPORTANT: If CutscenePlayer is in GameRoot (session-specific), this direct reference
    // assigned in the Boot scene's Inspector won't work reliably after GameRoot loads/unloads.
    // It's better to FIND or REGISTER the CutscenePlayer when GameRoot loads.
    // Let's remove the SerializeField and rely on registration.
    // [SerializeField] private CutscenePlayer cutscenePlayerInstance; // REMOVED - Cannot reliably assign across scenes this way

    /// <summary>
    /// Reference to the CutscenePlayer instance currently executing a cutscene. Null if none.
    /// Set via RegisterActiveCutscenePlayer.
    /// </summary>
    private CutscenePlayer activeCutscenePlayer = null;

    /// <summary>
    /// Gets whether a cutscene is currently considered active by the GameRunManager.
    /// </summary>
    public bool IsCutsceneActive => CurrentStatus == GameStatus.InCutscene;


    // --- Unity Lifecycle ---
    protected override void Awake() // Ensure Awake is protected virtual if overriding base Singleton
    {
        base.Awake(); // Call base Singleton Awake (which should handle DontDestroyOnLoad if applicable)

        // Ensure this manager persists across scenes if it's intended to be Application-Level
        // Make sure your Singleton base class handles DontDestroyOnLoad correctly, or add it here:
        // DontDestroyOnLoad(gameObject);

        // Set initial state
        CurrentStatus = GameStatus.InMenu; // Assuming game starts at the main menu

        // REMOVED validation for serialized field:
        // if (cutscenePlayerInstance == null) { ... }
    }

    void Update()
    {
        // Handle global cutscene skipping
        HandleCutsceneSkipInput();
    }

    // --- Public State Management Methods ---

    /// <summary>
    /// Changes the detailed game status and triggers events.
    /// </summary>
    /// <param name="newStatus">The new status to transition to.</param>
    public void ChangeGameStatus(GameStatus newStatus)
    {
        if (CurrentStatus == newStatus) return; // No change

        GameStatus previousStatus = CurrentStatus;
        CurrentStatus = newStatus;
        Debug.Log($"[GameRunManager] Game Status Changed: {previousStatus} -> {CurrentStatus}");

        // Handle side effects of the state change (like Time.timeScale)
        HandleStatusChange(previousStatus, newStatus);

        // Trigger the event for other systems to react (ensure EventManager exists)
        EventManager.Instance?.TriggerEvent(new GameStatusChangedEvent(CurrentStatus, previousStatus));
    }

    /// <summary>
    /// Pauses the game if currently playing.
    /// </summary>
    public void StartGameSession()
    {
        ChangeGameStatus(GameStatus.Playing);
        Debug.Log("StartGameSession called, game start.");
    }

    /// <summary>
    /// Pauses the game if currently playing.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentStatus == GameStatus.Playing)
        {
            ChangeGameStatus(GameStatus.Paused);
        }
        else
        {
             Debug.LogWarning($"PauseGame called but current status is {CurrentStatus}, not Playing.");
        }
    }

    /// <summary>
    /// Resumes the game if currently paused.
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentStatus == GameStatus.Paused)
        {
            ChangeGameStatus(GameStatus.Playing);
        }
         else
        {
             Debug.LogWarning($"ResumeGame called but current status is {CurrentStatus}, not Paused.");
        }
    }

    /// <summary>
    /// Ends the current game session, preparing to return to menu.
    /// Clears session state. Scene unloading/loading is handled externally.
    /// </summary>
    public void EndGameSession()
    {
        Debug.Log("[GameRunManager] Ending Game Session...");

        // If a cutscene was somehow active when ending session, stop it.
        if (IsCutsceneActive && activeCutscenePlayer != null)
        {
             Debug.LogWarning("Ending game session while cutscene was active. Stopping cutscene.");
             activeCutscenePlayer.StopCurrentCutscene(); // Stop it first
        }

        // Set state back to menu state
        ChangeGameStatus(GameStatus.InMenu);

        // Clear session-specific state
        SessionStateManager.Instance?.ClearAllSceneStates();

        // Ensure time scale is reset
        Time.timeScale = 1f;

        // The actual scene unloading/loading (Unload GameRoot, Load MainMenu)
        // should be triggered by the caller (e.g., ReturnToMenuHandler) AFTER this method returns.
    }


    // --- Cutscene Integration Methods ---

    /// <summary>
    /// Called by triggers or other systems to request a cutscene coroutine to play.
    /// Checks if a cutscene can start and validates the request.
    /// IMPORTANT: This method now relies on the activeCutscenePlayer being registered.
    /// It finds the player instance dynamically instead of using a serialized field.
    /// </summary>
    /// <param name="coroutineName">The string name of the public IEnumerator method in CutscenePlayer.</param>
    public void RequestCutscene(string coroutineName)
    {
        // 1. Check if already in a cutscene
        if (IsCutsceneActive)
        {
            Debug.LogWarning($"RequestCutscene('{coroutineName}') ignored: A cutscene is already active.");
            return;
        }

        // 2. Find the CutscenePlayer instance (assuming one exists in the loaded scenes)
        // This is less ideal than registration, but necessary if GameRunManager is persistent
        // and CutscenePlayer is session-specific. Consider finding it once when GameRoot loads?
        // For now, find it on request:
        CutscenePlayer playerInstance = FindAnyObjectByType<CutscenePlayer>(); // Find the active instance

        if (playerInstance == null)
        {
            Debug.LogError($"RequestCutscene('{coroutineName}') failed: No active CutscenePlayer instance found in the loaded scenes!");
            return;
        }

        // 3. Check if the coroutine name is valid
        if (string.IsNullOrEmpty(coroutineName))
        {
             Debug.LogError($"RequestCutscene failed: Coroutine name is null or empty!");
             return;
        }

        // 4. Optional but Recommended: Validate if the method exists on CutscenePlayer
        MethodInfo method = playerInstance.GetType().GetMethod(coroutineName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null || method.ReturnType != typeof(IEnumerator))
        {
             Debug.LogError($"RequestCutscene failed: Method '{coroutineName}' not found or is not an IEnumerator on {playerInstance.GetType().Name}!", playerInstance.gameObject);
             return;
        }

        // 5. Initiate the cutscene coroutine on the found CutscenePlayer instance
        Debug.Log($"[GameRunManager] Starting requested cutscene coroutine: '{coroutineName}' on {playerInstance.name}");
        playerInstance.StartCoroutine(coroutineName);
        // CutscenePlayer's RunCutsceneWrapper should call RegisterActiveCutscenePlayer shortly after this.
    }


    /// <summary>
    /// Called by CutscenePlayer when it starts executing a cutscene coroutine.
    /// Registers the player as the currently active one.
    /// </summary>
    /// <param name="player">The CutscenePlayer instance that started.</param>
    public void RegisterActiveCutscenePlayer(CutscenePlayer player)
    {
        if (player == null) { Debug.LogError("RegisterActiveCutscenePlayer called with null player!"); return; }

        // Check if another player is unexpectedly active
        if (activeCutscenePlayer != null && activeCutscenePlayer != player)
        {
            Debug.LogWarning($"New cutscene player '{player.name}' registered while '{activeCutscenePlayer.name}' was active. Overwriting reference.");
            // Force stop the old one? Could cause issues if not handled carefully.
            // activeCutscenePlayer.StopCurrentCutscene();
        }

        activeCutscenePlayer = player;
        Debug.Log($"[GameRunManager] Active cutscene player registered: {player.name}");

        // Ensure game state is InCutscene. ChangeGameStatus handles not changing if already set.
        ChangeGameStatus(GameStatus.InCutscene);
    }

    /// <summary>
    /// Called by CutscenePlayer when its cutscene coroutine finishes or is stopped.
    /// Unregisters the player if it's the currently active one.
    /// </summary>
    /// <param name="player">The CutscenePlayer instance that finished.</param>
    public void UnregisterActiveCutscenePlayer(CutscenePlayer player)
    {
        if (player == null) return;

        // Only unregister if the provided player is the one we currently track
        if (activeCutscenePlayer == player)
        {
            Debug.Log($"[GameRunManager] Unregistering active cutscene player: {player.name}");
            activeCutscenePlayer = null;

            // Safety check: If we're unregistering the active player, ensure state reverts.
            // This should ideally be handled by CutscenePlayer's finally block calling ChangeGameStatus first.
             if (CurrentStatus == GameStatus.InCutscene)
             {
                  Debug.LogWarning("CutscenePlayer unregistered, but GameStatus is still InCutscene. Reverting to Playing.");
                  ChangeGameStatus(GameStatus.Playing);
             }
        }
        else
        {
             Debug.LogWarning($"CutscenePlayer '{player.name}' tried to unregister, but it wasn't the tracked active player ('{activeCutscenePlayer?.name}').");
        }
    }

    /// <summary>
    /// Checks if the provided CutscenePlayer instance is the currently registered active one.
    /// Used as a safety check by CutscenePlayer before changing state back to Playing.
    /// </summary>
    /// <param name="player">The CutscenePlayer instance to check.</param>
    /// <returns>True if the provided player is the currently active one, false otherwise.</returns>
    public bool IsPlayerRegistered(CutscenePlayer player)
    {
        return player != null && activeCutscenePlayer == player;
    }

    /// <summary>
    /// Handles input for skipping cutscenes during the Update loop.
    /// </summary>
    private void HandleCutsceneSkipInput()
    {
        if (CurrentStatus == GameStatus.InCutscene)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) // Example skip keys
            {
                Debug.Log("Skip input detected by GameRunManager.");
                SkipCurrentCutscene();
            }
        }
    }

    /// <summary>
    /// Attempts to skip the currently active cutscene by telling the registered CutscenePlayer to stop.
    /// </summary>
    public void SkipCurrentCutscene()
    {
        if (CurrentStatus != GameStatus.InCutscene) { Debug.LogWarning("SkipCurrentCutscene called, but game status is not InCutscene."); return; }

        if (activeCutscenePlayer != null)
        {
            Debug.Log($"GameRunManager requesting active CutscenePlayer ('{activeCutscenePlayer.name}') to stop for skipping.");
            activeCutscenePlayer.StopCurrentCutscene(); // Tell the active player to stop
        }
        else
        {
            Debug.LogWarning("Skip requested, but no active CutscenePlayer is registered. Forcing state back to Playing.");
            ChangeGameStatus(GameStatus.Playing); // Force state back if something is wrong
        }
    }


    /// <summary>
    /// Handles logic associated with entering/exiting specific game states, like TimeScale.
    /// </summary>
    private void HandleStatusChange(GameStatus oldStatus, GameStatus newStatus)
    {
        // Handle Time Scale
        if (newStatus == GameStatus.Paused)
        {
            Time.timeScale = 0f;
        }
        // Ensure time scale is reset when exiting pause, or entering other normally-timed states
        else if (oldStatus == GameStatus.Paused || newStatus == GameStatus.Playing || newStatus == GameStatus.InCutscene || newStatus == GameStatus.InMenu)
        {
             if (Time.timeScale != 1f)
             {
                  Time.timeScale = 1f;
             }
        }
        // Note: InCutscene should generally run at Time.timeScale = 1
    }
}

// --- Remember required Enums and Events ---
// public enum GameStatus { InMenu, Loading, Playing, Paused, InDialogue, InCutscene, GameOver }
// public class GameStatusChangedEvent { public GameStatus NewStatus; public GameStatus PreviousStatus; /* ... */ }
// public class CutscenePlayer : MonoBehaviour { public void StopCurrentCutscene(); /* ... */ }

