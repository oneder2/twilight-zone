using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Plays cutscene sequences defined as Coroutines.
/// Registers itself with GameRunManager during playback.
/// Can be stopped externally via StopCurrentCutscene().
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    // isCutsceneActive flag might become redundant if GameRunManager.CurrentStatus is the source of truth,
    // but keeping it internally can help manage the coroutine state.
    private bool isCutsceneActive = false;
    private Coroutine currentCutsceneCoroutine = null;
    private IEnumerator currentSequence = null; // Store the actual sequence being run

    public bool IsCutscenePlaying => isCutsceneActive;

    // --- Public Methods to Start Specific Cutscenes ---
    public void PlayIntroSequence() { TryStartCutscene(IntroSequenceCoroutine()); }
    public void PlayEndingSequenceA() { TryStartCutscene(EndingSequenceACoroutine()); }
    // Add more...

    // --- Core Execution Logic ---
    private void TryStartCutscene(IEnumerator cutsceneSequence)
    {
        // Check global state via GameRunManager first
        if (GameRunManager.Instance != null && GameRunManager.Instance.IsCutsceneActive)
        {
             Debug.LogWarning($"CutscenePlayer: Cannot start '{cutsceneSequence.GetType().Name}', GameRunManager reports a cutscene is already active.");
             return;
        }
         // Check local state as well
        if (isCutsceneActive)
        {
            Debug.LogWarning("CutscenePlayer: Cannot start new cutscene, one is already playing locally.");
            return;
        }

        currentSequence = cutsceneSequence; // Store the sequence
        currentCutsceneCoroutine = StartCoroutine(RunCutsceneWrapper(currentSequence));
    }

    private IEnumerator RunCutsceneWrapper(IEnumerator actualCutscene)
    {
        isCutsceneActive = true;
        // Register with GameRunManager *before* changing state
        GameRunManager.Instance?.RegisterActiveCutscenePlayer(this);
        Debug.Log($"Cutscene Wrapper ({gameObject.name}): Starting '{actualCutscene.GetType().Name}'. Registering with GameRunManager. Taking control.");

        // Set Game State (disables player input etc.)
        GameRunManager.Instance?.ChangeGameStatus(GameStatus.InCutscene);
        yield return null; // Wait a frame

        // Run the actual cutscene
        // Use try-finally to ensure cleanup happens even if the cutscene coroutine throws an exception
        try
        {
            yield return StartCoroutine(actualCutscene);
        }
        finally // This block will execute whether the coroutine finishes normally, is stopped, or throws an error
        {
            Debug.Log($"Cutscene Wrapper ({gameObject.name}): Sequence '{actualCutscene.GetType().Name}' finished or stopped. Performing cleanup.");

            // Check if we are still considered the active player before changing state back
            // This prevents issues if another cutscene started immediately after this one was stopped externally
            if (GameRunManager.Instance != null && GameRunManager.Instance.IsCutsceneActive && GameRunManager.Instance.IsPlayerRegistered(this)) // Need IsPlayerRegistered method
            {
                 // Return control only if we were the active one
                 GameRunManager.Instance.ChangeGameStatus(GameStatus.Playing);
            }
             else {
                  Debug.LogWarning($"Cutscene Wrapper ({gameObject.name}): Cleanup running, but GameStatus was not InCutscene or this wasn't the registered player. State remains: {GameRunManager.Instance?.CurrentStatus}");
             }


            // Unregister AFTER potentially changing the state back
            GameRunManager.Instance?.UnregisterActiveCutscenePlayer(this);

            isCutsceneActive = false;
            currentCutsceneCoroutine = null;
            currentSequence = null; // Clear stored sequence
            Debug.Log($"Cutscene Wrapper ({gameObject.name}): Cleanup complete.");
        }
    }

    /// <summary>
    /// Stops the currently running cutscene coroutine immediately and triggers cleanup.
    /// Called by GameRunManager when skipping.
    /// </summary>
    public void StopCurrentCutscene()
    {
        if (currentCutsceneCoroutine != null)
        {
            Debug.Log($"CutscenePlayer ({gameObject.name}): Stop requested. Stopping coroutine '{currentSequence?.GetType().Name}'.");
            StopCoroutine(currentCutsceneCoroutine); // Stop the wrapper

            // The 'finally' block in RunCutsceneWrapper should handle the cleanup,
            // including changing game state and unregistering.
            // We might need to manually trigger parts of the cleanup if StopCoroutine doesn't guarantee 'finally' runs instantly in all cases,
            // but typically it should. Let's rely on 'finally' for now.

            // Reset local state immediately just in case
            isCutsceneActive = false;
            currentCutsceneCoroutine = null;
            // currentSequence = null; // Keep sequence ref until finally runs? Maybe clear here.
        }
        else
        {
            Debug.LogWarning($"CutscenePlayer ({gameObject.name}): Stop requested, but no coroutine was running.");
        }
    }


    // --- Example Sequence Coroutines (Keep as before) ---
    private IEnumerator IntroSequenceCoroutine() { /* ... sequence logic ... */ yield break; }
    private IEnumerator EndingSequenceACoroutine() { /* ... sequence logic ... */ yield break; }

    // --- Helper Coroutines (Keep as before) ---
    private IEnumerator ShowDialogueAndWait(string line) { /* ... */ yield break; }
    private IEnumerator ShowDialogueAndWait(string[] lines) { /* ... */ yield break; }
    private IEnumerator MoveObjectCoroutine(Transform objectToMove, Vector3 targetPosition, float duration) { /* ... */ yield break; }

}

// --- Add this method to GameRunManager.cs for the check in CutscenePlayer ---
/*
// In GameRunManager.cs
/// <summary>
/// Checks if the provided CutscenePlayer instance is the currently registered active one.
/// </summary>
public bool IsPlayerRegistered(CutscenePlayer player)
{
    return activeCutscenePlayer == player;
}
*/
