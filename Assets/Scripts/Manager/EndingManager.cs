using UnityEngine;
using UnityEngine.Playables; // If endings use Timelines
using UnityEngine.SceneManagement; // If endings load scenes

/// <summary>
/// Determines and triggers the appropriate game ending based on player progress.
/// 根据玩家进度确定并触发适当的游戏结局。
/// Listens for the EndingCheckRequestedEvent.
/// 监听 EndingCheckRequestedEvent。
/// Should be a persistent Singleton (e.g., in "Boot" scene).
/// 应该是持久化的 Singleton（例如，在“Boot”场景中）。
/// </summary>
public class EndingManager : Singleton<EndingManager>
{
    [Header("Ending Sequences (Assign Directors/Scene Names)")]
    [Tooltip("Timeline or Scene Name for the 'Good' ending.")]
    // [Tooltip("'好' 结局的 Timeline 或场景名称。")]
    [SerializeField] private PlayableDirector goodEndingDirector;
    // [SerializeField] private string goodEndingSceneName;

    [Tooltip("Timeline or Scene Name for the 'Bad' ending.")]
    // [Tooltip("'坏' 结局的 Timeline 或场景名称。")]
    [SerializeField] private PlayableDirector badEndingDirector;
    // [SerializeField] private string badEndingSceneName;

    // Add more endings as needed
    // 根据需要添加更多结局

    private bool endingTriggered = false; // Prevent multiple ending triggers per session

    protected override void Awake()
    {
        base.Awake();
        endingTriggered = false; // Reset on awake
    }

    void OnEnable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<EndingCheckRequestedEvent>(HandleEndingCheckRequest);
            Debug.Log("[EndingManager] Subscribed to EndingCheckRequestedEvent.");
        } else { Debug.LogError("[EndingManager] EventManager not found on Enable!"); }
    }

    void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<EndingCheckRequestedEvent>(HandleEndingCheckRequest);
            Debug.Log("[EndingManager] Unsubscribed from EndingCheckRequestedEvent.");
        }
    }

    /// <summary>
    /// Handles the request to check and trigger an ending.
    /// 处理检查并触发结局的请求。
    /// </summary>
    private void HandleEndingCheckRequest(EndingCheckRequestedEvent eventData)
    {
        if (endingTriggered)
        {
            Debug.LogWarning("[EndingManager] Ending check requested, but an ending has already been triggered this session.");
            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError("[EndingManager] ProgressManager not found! Cannot determine ending.");
            return; // Cannot proceed without progress data
        }

        endingTriggered = true; // Mark that we are processing the ending
        Debug.Log("[EndingManager] Handling ending check request...");

        // --- Determine Ending Logic ---
        // TODO: Implement detailed logic based on ProgressManager flags
        bool goodEndingCondition = CheckGoodEndingConditions();
        bool badEndingCondition = CheckBadEndingConditions(); // Example

        if (goodEndingCondition)
        {
            TriggerEndingSequence(goodEndingDirector, "Good Ending");
        }
        else if (badEndingCondition)
        {
            TriggerEndingSequence(badEndingDirector, "Bad Ending");
        }
        else
        {
            // Default or Neutral Ending?
            Debug.LogWarning("[EndingManager] No specific ending conditions met. Triggering default/bad ending.");
            TriggerEndingSequence(badEndingDirector, "Default/Bad Ending"); // Fallback to bad?
        }
        // --- End Ending Logic ---
    }

    /// <summary>
    /// Placeholder: Checks conditions for the good ending.
    /// 占位符：检查好结局的条件。
    /// </summary>
    /// <returns>True if conditions are met.</returns>
    private bool CheckGoodEndingConditions()
    {
        // Example: Check if all characters were dealt with 'correctly'
        return ProgressManager.Instance.BeginnerOutcome == CharacterOutcome.KilledStandard &&
               ProgressManager.Instance.CrushsisOutcome == CharacterOutcome.KilledStandard && // Assuming standard = good outcome? Adjust as needed
               ProgressManager.Instance.FriendOutcome == CharacterOutcome.KilledStandard &&
               ProgressManager.Instance.CrushOutcome == CharacterOutcome.KilledStandard &&
               ProgressManager.Instance.TeacherOutcome == CharacterOutcome.KilledStandard && // Teacher must be killed by player
               ProgressManager.Instance.CheckedTeacherEvidenceCorrectly; // And evidence handled well
    }

    /// <summary>
    /// Placeholder: Checks conditions for a bad ending.
    /// 占位符：检查坏结局的条件。
    /// </summary>
    /// <returns>True if conditions are met.</returns>
    private bool CheckBadEndingConditions()
    {
        // Example: Teacher committed suicide
        return ProgressManager.Instance.TeacherOutcome == CharacterOutcome.Suicide;
    }


    /// <summary>
    /// Triggers the specified ending sequence (Timeline or Scene Load).
    /// 触发指定的结局序列（Timeline 或场景加载）。
    /// </summary>
    /// <param name="endingDirector">The PlayableDirector for the ending Timeline (optional).</param>
    /// <param name="endingName">A descriptive name for logging.</param>
    private void TriggerEndingSequence(PlayableDirector endingDirector, string endingName)
    {
        Debug.Log($"[EndingManager] Triggering: {endingName}");

        // Option 1: Play Ending Timeline
        if (endingDirector != null)
        {
            // Ensure game state allows ending (e.g., not paused)
            if (GameRunManager.Instance != null)
            {
                // Maybe set a specific GameStatus.InEnding state?
                // GameRunManager.Instance.ChangeGameStatus(GameStatus.InEnding); // Needs enum update
                GameRunManager.Instance.ChangeGameStatus(GameStatus.InCutscene); // Use InCutscene for now
            }

            endingDirector.Play();
            // Add listener to return to menu after ending timeline finishes
            endingDirector.stopped += ReturnToMenuAfterEnding;
        }
        // Option 2: Load Ending Scene (Not implemented here)
        // else if (!string.IsNullOrEmpty(endingSceneName)) { ... SceneManager.LoadSceneAsync ... }
        else
        {
            Debug.LogWarning($"[EndingManager] No Director assigned for '{endingName}'. Returning to menu directly.");
            ReturnToMenuAfterEnding(null); // Go straight to menu if no sequence
        }
    }

    /// <summary>
    /// Called after an ending Timeline finishes playing. Returns to the main menu.
    /// 在结局 Timeline 播放完成后调用。返回主菜单。
    /// </summary>
    private void ReturnToMenuAfterEnding(PlayableDirector director)
    {
        Debug.Log("[EndingManager] Ending sequence finished. Returning to Main Menu.");
        if (director != null) director.stopped -= ReturnToMenuAfterEnding; // Unsubscribe

        endingTriggered = false; // Allow triggering again if player starts a new game

        // Use GameRunManager or EndGameManager to handle the transition properly
        if (GameRunManager.Instance != null)
        {
            // Option A: Use existing flow
            GameRunManager.InitiateRestartFlow = false; // Ensure no auto-restart
            EventManager.Instance?.TriggerEvent(new GameEndEvent()); // Trigger the standard end sequence

            // Option B: Direct call if needed (less clean)
            // GameRunManager.Instance.EndGameSession();
            // SceneManager.LoadSceneAsync(mainMenuSceneName); // Needs scene name variable
        } else {
            Debug.LogError("[EndingManager] GameRunManager not found! Cannot return to menu.");
            // Fallback?
            // SceneManager.LoadScene(0); // Load scene by index 0? Risky.
        }
    }

    // Ensure listener is removed if the object is destroyed
    void OnDestroy()
    {
        if (goodEndingDirector != null) goodEndingDirector.stopped -= ReturnToMenuAfterEnding;
        if (badEndingDirector != null) badEndingDirector.stopped -= ReturnToMenuAfterEnding;
        // Unsubscribe from other directors if added
    }
}
