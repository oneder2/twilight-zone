using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

/// <summary>
/// Generic player for executing CutsceneSequence ScriptableObject assets.
/// Manages the overall playback lifecycle, state changes, and skip handling.
/// Provides helper coroutines for common cutscene actions.
/// Should reside in GameRoot scene.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    // --- References to Managers (Public properties for sequences to access) ---
    // Fetched in Start()
    public GameRunManager GameRunManager { get; private set; }
    public CutsceneUIManager CutsceneUIManager { get; private set; }
    public CameraManager CameraManager { get; private set; }
    public DialogueGUI DialogueGUI { get; private set; }
    // GameSceneManager is found dynamically per sequence if needed

    // --- Cutscene State ---
    private bool isCutsceneActive = false;
    private Coroutine currentCutsceneCoroutine = null;
    private CutsceneSequence currentSequenceAsset = null; // Track the SO being played
    public bool skipRequested { get; private set; } = false; // Public getter for sequences to check

    /// <summary>
    /// Gets whether any cutscene managed by this player is currently running.
    /// </summary>
    public bool IsCutscenePlaying => isCutsceneActive;

    // --- Initialization ---
    void Start()
    {
        // Get manager references reliably
        GameRunManager = GameRunManager.Instance;
        CutsceneUIManager = CutsceneUIManager.Instance;
        CameraManager = CameraManager.Instance;
        DialogueGUI = DialogueGUI.Instance;

        if (GameRunManager == null || CutsceneUIManager == null || CameraManager == null || DialogueGUI == null)
        {
            Debug.LogError($"CutscenePlayer on {gameObject.name} could not find required Manager Instances on Start!", this);
            enabled = false;
        }
    }

    // --- Public Method to Start a Sequence Asset ---

    /// <summary>
    /// Starts playing a cutscene defined by a CutsceneSequence ScriptableObject asset.
    /// Called by GameRunManager.
    /// </summary>
    /// <param name="sequenceAsset">The ScriptableObject asset defining the cutscene.</param>
    public void PlaySequence(CutsceneSequence sequenceAsset)
    {
        if (sequenceAsset == null)
        {
            Debug.LogError("PlaySequence called with a null CutsceneSequence asset!");
            return;
        }
        TryStartCutscene(sequenceAsset);
    }


    // --- Core Execution Logic ---

    private void TryStartCutscene(CutsceneSequence sequenceAsset)
    {
        if (GameRunManager != null && GameRunManager.IsCutsceneActive) { Debug.LogWarning($"CutscenePlayer: Cannot start '{sequenceAsset.name}', GameRunManager reports a cutscene is already active."); return; }
        if (isCutsceneActive) { Debug.LogWarning("CutscenePlayer: Cannot start new cutscene, one is already playing locally."); return; }
        if (GameRunManager == null || DialogueGUI == null || CutsceneUIManager == null || CameraManager == null) { Debug.LogError("Cannot start cutscene - required managers not found!", this); return; }

        currentSequenceAsset = sequenceAsset; // Store the asset being played
        currentCutsceneCoroutine = StartCoroutine(RunCutsceneWrapper(currentSequenceAsset));
    }

    private IEnumerator RunCutsceneWrapper(CutsceneSequence sequenceAsset)
    {
        isCutsceneActive = true;
        skipRequested = false;
        string sequenceName = sequenceAsset?.name ?? "UnknownSequence";

        GameRunManager?.RegisterActiveCutscenePlayer(this);
        Debug.Log($"Cutscene Wrapper ({gameObject.name}): Starting '{sequenceName}'. Registering. Taking control.");
        GameRunManager?.ChangeGameStatus(GameStatus.InCutscene);
        yield return null;

        try
        {
            // Execute the Play method from the ScriptableObject
            yield return StartCoroutine(sequenceAsset.Play(this));
        }
        // catch (System.Exception e) { Debug.LogError($"Exception during cutscene '{sequenceName}': {e.Message}\n{e.StackTrace}"); }
        finally
        {
            Debug.Log($"Cutscene Wrapper ({gameObject.name}): Sequence '{sequenceName}' finished or stopped. Performing cleanup.");

            // Apply final state ONLY if skip was requested
            if (skipRequested) { /* ApplyFinalStateForSkip(); // Called directly by StopCurrentCutscene now */ }

            bool wasRegisteredAndActive = GameRunManager != null && GameRunManager.IsPlayerRegistered(this) && GameRunManager.CurrentStatus == GameStatus.InCutscene;
            if (wasRegisteredAndActive) { GameRunManager.ChangeGameStatus(GameStatus.Playing); }
            else { Debug.LogWarning($"Cutscene Wrapper ({gameObject.name}): Cleanup running, but state was not InCutscene or this wasn't registered player."); }

            GameRunManager?.UnregisterActiveCutscenePlayer(this); // Unregister self

            // Ensure UI is hidden on normal exit (skip hides them in ApplyFinalState)
             if (!skipRequested && CutsceneUIManager != null)
             {
                 CutsceneUIManager.HideLetterbox(false);
                 CutsceneUIManager.HideFullscreenImage(0f);
             }

            isCutsceneActive = false;
            currentCutsceneCoroutine = null;
            currentSequenceAsset = null; // Clear asset reference
            skipRequested = false;
            Debug.Log($"Cutscene Wrapper ({gameObject.name}): Cleanup complete.");
        }
    }

    /// <summary>
    /// Stops the currently running cutscene coroutine immediately and applies the final state via the sequence asset.
    /// Called by GameRunManager when skipping.
    /// </summary>
    public void StopCurrentCutscene()
    {
        if (!isCutsceneActive || currentCutsceneCoroutine == null || currentSequenceAsset == null) { Debug.LogWarning($"CutscenePlayer ({gameObject.name}): Stop requested, but no coroutine/sequence was active."); return; }
        string sequenceName = currentSequenceAsset.name;
        Debug.Log($"CutscenePlayer ({gameObject.name}): Stop requested for '{sequenceName}'. Stopping coroutine and applying final state.");
        skipRequested = true;

        StopCoroutine(currentCutsceneCoroutine);

        // Apply final state immediately using the sequence asset's method
        ApplyFinalStateForSkip();

        // Reset state immediately
        isCutsceneActive = false;
        currentCutsceneCoroutine = null;
        // currentSequenceAsset = null; // Keep reference for ApplyFinalStateForSkip? Cleared in ApplyFinalState.
        // Game state change and unregistration handled by ApplyFinalStateForSkip
    }

    /// <summary>
    /// Instantly applies the final state of the current sequence asset, used when skipping.
    /// </summary>
    private void ApplyFinalStateForSkip()
    {
         if (currentSequenceAsset != null)
         {
              Debug.Log($"Applying final state for skipped sequence asset: {currentSequenceAsset.name}");
              // Call the method defined in the ScriptableObject
              currentSequenceAsset.ApplySkipState(this);
         }
         else { Debug.LogWarning("ApplyFinalStateForSkip called but currentSequenceAsset is null!"); }

         // General Cleanup for Skip - Change state back and unregister
         bool wasRegistered = GameRunManager != null && GameRunManager.IsPlayerRegistered(this);
         if (wasRegistered && GameRunManager.CurrentStatus == GameStatus.InCutscene) { GameRunManager.ChangeGameStatus(GameStatus.Playing); }
         else { Debug.LogWarning("Skip Cleanup: State wasn't InCutscene or wasn't registered player."); }
         GameRunManager?.UnregisterActiveCutscenePlayer(this); // Ensure unregistration on skip

         // Clear asset reference after applying skip state
         currentSequenceAsset = null;
         skipRequested = false; // Reset skip flag after handling
    }


    // --- Public Helper Coroutines (Callable by CutsceneSequence Assets) ---

    /// <summary>
    /// Helper to show dialogue and wait for it to complete or be skipped.
    /// </summary>
    public IEnumerator ShowDialogueAndWait(string line)
    {
        if (DialogueGUI != null) { DialogueGUI.ShowDialogue(line); yield return new WaitUntil(() => skipRequested || (DialogueGUI != null && !DialogueGUI.IsDialogueActive)); if (skipRequested && DialogueGUI != null && DialogueGUI.IsDialogueActive) DialogueGUI.HideDialogue(); yield return new WaitForSeconds(0.1f); }
        else { Debug.LogError("DialogueGUI instance not found!"); yield return new WaitForSeconds(1.0f); }
    }
    /// <summary>
    /// Helper to show dialogue and wait for it to complete or be skipped.
    /// </summary>
    public IEnumerator ShowDialogueAndWait(string[] lines)
    {
        if (DialogueGUI != null) { DialogueGUI.ShowDialogue(lines); yield return new WaitUntil(() => skipRequested || (DialogueGUI != null && !DialogueGUI.IsDialogueActive)); if (skipRequested && DialogueGUI != null && DialogueGUI.IsDialogueActive) DialogueGUI.HideDialogue(); yield return new WaitForSeconds(0.1f); }
        else { Debug.LogError("DialogueGUI instance not found!"); yield return new WaitForSeconds(1.0f * (lines?.Length ?? 1)); }
    }

    /// <summary>
    /// Helper coroutine for smoothly moving a Transform to a target position over a duration.
    /// Checks for skip request during movement.
    /// </summary>
    public IEnumerator MoveObjectCoroutine(Transform objectToMove, Vector3 targetPosition, float duration)
    {
        if (objectToMove == null || duration <= 0) { Debug.LogWarning("MoveObjectCoroutine: Invalid parameters."); yield break; }
        Vector3 startPosition = objectToMove.position; float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
             if (objectToMove == null) { Debug.LogWarning("Object to move was destroyed during MoveObjectCoroutine."); yield break; } // Object destroyed
             if (skipRequested) { Debug.Log("MoveObjectCoroutine skipped."); yield break; } // Skip requested

             elapsedTime += Time.deltaTime;
             float progress = Mathf.Clamp01(elapsedTime / duration);
             objectToMove.position = Vector3.Lerp(startPosition, targetPosition, progress);
             yield return null;
        }
        // Set final position only if not skipped and object still exists
        if (objectToMove != null && !skipRequested) objectToMove.position = targetPosition;
    }

    /// <summary>
    /// Helper to find the GameSceneManager in the currently active scene.
    /// </summary>
    public GameSceneManager GetCurrentGameSceneManager()
    {
         // Cache it? Or find each time? Finding each time is safer if active scene changes unexpectedly.
         return FindFirstObjectByType<GameSceneManager>(); // Assumes only one GSC active
    }
}
