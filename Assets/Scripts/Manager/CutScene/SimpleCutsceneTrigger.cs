using UnityEngine;

/// <summary>
/// A simple example trigger that requests a specific CutsceneSequence asset
/// via the GameRunManager when triggered (e.g., by player entering a collider).
/// Attach this to a GameObject in your Level scene.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SimpleCutsceneTrigger : MonoBehaviour
{
    // REMOVED: [SerializeField] private string cutsceneCoroutineName;

    [Tooltip("The CutsceneSequence asset file to play when triggered.")]
    [SerializeField] private CutsceneSequence sequenceToPlay; // Assign your .asset file here

    [Tooltip("Should this trigger only activate once?")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Layer mask to specify what layers can trigger this cutscene (e.g., Player layer).")]
    [SerializeField] private LayerMask triggeringLayer; // Set this in the Inspector

    private bool hasTriggered = false;

    void Awake()
    {
        // Validation
        if (sequenceToPlay == null)
        {
             Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Cutscene Sequence asset to be assigned!", gameObject);
             enabled = false;
        }

        // Ensure collider is trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        else { Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Collider2D.", gameObject); enabled = false; }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered) return;
        if (triggeringLayer.value != 0 && ((1 << other.gameObject.layer) & triggeringLayer) == 0) return;
        if (GameRunManager.Instance == null) { Debug.LogError("GameRunManager instance not found!", gameObject); return; }
        if (GameRunManager.Instance.IsCutsceneActive) { Debug.LogWarning($"Cutscene trigger on {gameObject.name} activated, but a cutscene is already active."); return; }

        Debug.Log($"{gameObject.name} triggered by {other.name}. Requesting cutscene sequence asset '{sequenceToPlay?.name}' via GameRunManager.");

        // --- Request the cutscene via GameRunManager, passing the asset ---
        if (sequenceToPlay != null)
        {
            GameRunManager.Instance.RequestCutscene(sequenceToPlay.ToString()); // Pass the ScriptableObject asset
            if (triggerOnce) { hasTriggered = true; /* gameObject.SetActive(false); */ }
        }
        else {
             Debug.LogError("Sequence To Play asset is not assigned!", this.gameObject);
        }
    }
}
