using System.Collections;
using UnityEngine;


/// <summary>
/// Example implementation for the 'crush_success' cutscene sequence.
/// Create instances of this via Assets -> Create -> Cutscenes -> Crush Success Sequence.
/// </summary>
[CreateAssetMenu(fileName = "CrushSuccessSequence", menuName = "Cutscenes/Crush Success Sequence")]
public class CrushSuccessSequence : CutsceneSequence
{
    // --- Sequence Specific Data (Moved from CutscenePlayer) ---
    // Assign these in the Inspector of the created .asset file
    public Sprite fullscreenCG1; // Assign your PNG/Sprite asset here
    [Tooltip("Dialogue lines for the second part (during fullscreen CG).")]
    public string[] dialoguePart1 = { "It's starting!", "What's going to happen?" };
    [Tooltip("The fullscreen image (Sprite) to display.")]
    public Sprite fullscreenCG2; // Assign your PNG/Sprite asset here
    [Tooltip("Dialogue lines for the second part (during fullscreen CG).")]
    public string[] dialoguePart2 = { "...", "It's done." };
    [Tooltip("The final fullscreen image (Sprite) showing the result.")]
    public Sprite finalResultCG; // Assign the final result image asset here
    [Tooltip("A key/identifier passed to GameSceneManager to apply final object states.")]
    public string postCutsceneObjectStateKey = "CrushSuccessOutcome";


    /// <summary>
    /// Executes the 'crush_success' sequence logic.
    /// </summary>
    public override IEnumerator Play(CutscenePlayer player)
    {
        Debug.Log($"Cutscene Sequence: '{this.name}' Started!");

        // --- Find Scene Objects dynamically ---
        // ScriptableObjects cannot directly reference scene objects persistently.
        // We need to find them when the sequence starts, using the IDs/Tags set on the asset.
        // This requires GameObjects in the scene to be findable (e.g., have unique names/tags).
        Animator currentCrushAnimator = FindSceneObject<Animator>(crushObjectSceneID);
        Transform currentPlayerTarget = FindSceneObject<Transform>(playerMoveTargetSceneID);
        Transform currentObjectTarget = FindSceneObject<Transform>(objectMoveTargetSceneID); // Optional
        Transform currentPlayerStart = FindSceneObject<Transform>(playerStartSceneID); // Optional
        Transform playerTransform = Player.Instance?.transform; // Assumes Player Singleton still

        // --- Sequence Steps ---

        // Step 1: Focus camera on crush object
        Debug.Log("Step 1: Focus Camera");
        if (currentCrushAnimator != null && player.CameraManager != null) // Access managers via player
        {
            player.CameraManager.FocusOnTarget(currentCrushAnimator.transform, 0.5f);
            yield return new WaitForSeconds(0.6f);
        } else { Debug.LogWarning("Crush object animator or CameraManager missing for Step 1."); }
        if (player.skipRequested) yield break;

        // Step 2: Play crush object animation
        Debug.Log("Step 2: Play Crush Animation 1");
        if (currentCrushAnimator != null && !string.IsNullOrEmpty(crushAnimState1)) { currentCrushAnimator.Play(crushAnimState1); }
        else { Debug.LogWarning("Crush object animator or AnimState1 name missing for Step 2."); }

        // Step 3: Move Player
        Debug.Log("Step 3: Move Player");
        if (playerTransform != null && currentPlayerStart != null) { playerTransform.position = currentPlayerStart.position; }
        if (playerTransform != null && currentPlayerTarget != null) { yield return player.StartCoroutine(player.MoveObjectCoroutine(playerTransform, currentPlayerTarget.position, 3.0f)); }
        else { Debug.LogWarning("Player or Player target/start missing for movement."); }
        if (player.skipRequested) yield break;
        Debug.Log("Step 3: Player Movement complete.");

        // Step 4: Dialogue Part 1
        Debug.Log("Step 4: Dialogue Part 1");
        if (dialoguePart1 != null && dialoguePart1.Length > 0) { yield return player.StartCoroutine(player.ShowDialogueAndWait(dialoguePart1)); }
        if (player.skipRequested) yield break;

        // Step 5: Change crush object animation state
        Debug.Log("Step 5: Play Crush Animation 2");
        if (currentCrushAnimator != null && !string.IsNullOrEmpty(crushAnimState2)) { currentCrushAnimator.Play(crushAnimState2); yield return new WaitForSeconds(0.5f); }
        else { Debug.LogWarning("Crush object animator or AnimState2 name missing for Step 5."); }
        if (player.skipRequested) yield break;

        // Step 6: Dialogue Part 2
        Debug.Log("Step 6: Dialogue Part 2");
        if (dialoguePart2 != null && dialoguePart2.Length > 0) { yield return player.StartCoroutine(player.ShowDialogueAndWait(dialoguePart2)); }
        if (player.skipRequested) yield break;

        // Step 7: Enter Fullscreen CG
        Debug.Log("Step 7: Show Fullscreen CG");
        if (player.CutsceneUIManager != null && fullscreenCG != null) { yield return player.CutsceneUIManager.ShowFullscreenImage(fullscreenCG, 0.5f); }
        else { Debug.LogWarning("CutsceneUIManager or Fullscreen CG sprite missing."); yield return null;}
        if (player.skipRequested) yield break;

        // Step 8: Interspersed CG/Dialogue
        Debug.Log("Step 8: More Dialogue over CG");
        string[] dialoguePart3 = {"The result is clear."};
        yield return player.StartCoroutine(player.ShowDialogueAndWait(dialoguePart3));
        if (player.skipRequested) yield break;

        // Step 9: Hide current CG, Apply State, Show Result CG
        Debug.Log("Step 9: Hide current CG, Apply State, Show Result CG");
        if (player.CutsceneUIManager != null) { yield return player.CutsceneUIManager.HideFullscreenImage(0.3f); }
        if (player.skipRequested) yield break;

        // Apply the post-cutscene state changes
        GameSceneManager currentGSM = player.GetCurrentGameSceneManager(); // Need player to provide this
        if (currentGSM != null) { currentGSM.ApplyPostCutsceneState(postCutsceneObjectStateKey); }
        else { Debug.LogWarning("Could not find GameSceneManager to apply post-CG state."); }
        yield return null;

        // Show the final result image
        if (player.CutsceneUIManager != null && finalResultCG != null) { yield return player.CutsceneUIManager.ShowFullscreenImage(finalResultCG, 0.1f); }
        else { Debug.LogWarning("Final Result CG sprite missing."); }
        if (player.skipRequested) yield break;

        // Step 10: Restore vision
        Debug.Log("Step 10: Restore Vision");
        yield return new WaitForSeconds(2.0f);
        if (player.skipRequested) yield break;
        if (player.CutsceneUIManager != null) { yield return player.CutsceneUIManager.HideFullscreenImage(0.5f); }
        if (player.skipRequested) yield break;
        if (player.CameraManager != null) player.CameraManager.ReturnToPlayerFollow(0.5f);
        yield return new WaitForSeconds(0.6f);
        if (player.skipRequested) yield break;

        // Step 11: End CG (Wrapper handles state change), Hide Letterbox
        Debug.Log("Step 11: Hide Letterbox");
        if (player.CutsceneUIManager != null) yield return player.CutsceneUIManager.HideLetterbox(true);

        Debug.Log($"Cutscene Sequence: '{this.name}' Finished normally.");
    }


    /// <summary>
    /// Applies the final state instantly when this sequence is skipped.
    /// </summary>
    public override void ApplySkipState(CutscenePlayer player)
    {
        Debug.Log($"Applying final skip state for sequence: {this.name}");

        // Find objects dynamically again
        Animator currentCrushAnimator = FindSceneObject<Animator>(crushObjectSceneID);
        Transform currentPlayerTarget = FindSceneObject<Transform>(playerMoveTargetSceneID);
        // Transform currentObjectTarget = FindSceneObject<Transform>(objectMoveTargetSceneID); // If object moves
        Transform playerTransform = Player.Instance?.transform;

        // 1. Instantly move objects to final positions
        if (playerTransform != null && currentPlayerTarget != null) playerTransform.position = currentPlayerTarget.position;
        // if (currentCrushAnimator != null && currentObjectTarget != null) currentCrushAnimator.transform.position = currentObjectTarget.position; // If object moves
        Debug.Log("Skip: Objects moved instantly.");

        // 2. Instantly apply scene object changes via GameSceneManager
        GameSceneManager currentGSM = player.GetCurrentGameSceneManager();
        if (currentGSM != null)
        {
            currentGSM.ApplyPostCutsceneState(postCutsceneObjectStateKey);
            Debug.Log("Skip: Post-cutscene state applied instantly.");
        } else { Debug.LogWarning("Skip: Could not find GameSceneManager to apply post-cutscene state."); }

        // 3. Ensure UI is hidden instantly (Dialogue handled by player's ShowDialogueAndWait check)
        player.CutsceneUIManager?.HideLetterbox(false);
        player.CutsceneUIManager?.HideFullscreenImage(0f);
    }

    // Helper to find objects in the scene based on name/tag/ID provided in the SO asset.
    // You might need a more robust finding mechanism (e.g., a scene registry).
    private T FindSceneObject<T>(string identifier) where T : Component
    {
         if (string.IsNullOrEmpty(identifier)) return null;
         GameObject foundObj = GameObject.Find(identifier); // Simple find by name
         if (foundObj != null)
         {
              return foundObj.GetComponent<T>();
         }
         // Could also try FindWithTag if using tags
         Debug.LogWarning($"Could not find scene object with identifier '{identifier}' for sequence '{this.name}'.");
         return null;
    }
}
