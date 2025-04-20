using UnityEngine;

/// <summary>
/// A simple example trigger that requests a specific cutscene coroutine
/// via the GameRunManager when triggered (e.g., by player entering a collider).
/// Attach this to a GameObject in your Level scene.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SimpleCutsceneTrigger : MonoBehaviour
{
    // REMOVED: No longer holds a direct reference to CutscenePlayer
    // [SerializeField] private CutscenePlayer cutscenePlayer;

    [Tooltip("The exact name of the public IEnumerator method in CutscenePlayer to start (e.g., 'PlayIntroSequence'). Case sensitive!")]
    [SerializeField] private string cutsceneCoroutineName;

    [Tooltip("Should this trigger only activate once?")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Layer mask to specify what layers can trigger this cutscene (e.g., Player layer).")]
    [SerializeField] private LayerMask triggeringLayer; // Set this in the Inspector

    private bool hasTriggered = false;

    void Awake()
    {
        // Basic validation for required fields set in Inspector
        if (string.IsNullOrEmpty(cutsceneCoroutineName))
        {
             Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Cutscene Coroutine Name to be set in the Inspector!", gameObject);
             enabled = false; // Disable if not configured
        }

        // Ensure collider is set to be a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
             col.isTrigger = true;
        }
        else
        {
             Debug.LogError($"SimpleCutsceneTrigger on {gameObject.name} requires a Collider2D component.", gameObject);
             enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check if it should trigger only once and has already triggered
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        // 2. Check if the entering object belongs to the triggering layer
        // If triggeringLayer is not set (value 0), it allows all layers.
        if (triggeringLayer.value != 0 && ((1 << other.gameObject.layer) & triggeringLayer) == 0)
        {
             // Uncomment the line below if you want strict layer checking
             // return;
        }

        // 3. Check if GameRunManager exists and if a cutscene is already active via the manager
        if (GameRunManager.Instance == null)
        {
             Debug.LogError("GameRunManager instance not found! Cannot request cutscene.", gameObject);
             return;
        }
        if (GameRunManager.Instance.IsCutsceneActive)
        {
             Debug.LogWarning($"Cutscene trigger on {gameObject.name} activated, but GameRunManager reports a cutscene is already active.");
             return; // Don't trigger if another cutscene is playing
        }


        Debug.Log($"{gameObject.name} triggered by {other.name}. Requesting cutscene coroutine '{cutsceneCoroutineName}' via GameRunManager.");

        // 4. Request the cutscene via GameRunManager
        // Pass the name of the coroutine method to the manager
        GameRunManager.Instance.RequestCutscene(cutsceneCoroutineName);

        // 5. Mark as triggered if set to trigger once
        // We assume the request will likely succeed if GameRunManager exists and isn't busy.
        // For more robustness, RequestCutscene could return a bool.
        if (triggerOnce)
        {
            hasTriggered = true;
            // Optional: Disable this trigger after use
            // gameObject.SetActive(false);
        }
    }
}
