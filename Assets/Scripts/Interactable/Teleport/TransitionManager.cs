using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// (Make sure GameSceneManager and SessionStateManager classes are defined and accessible)
// (Make sure SceneSaveData from DataStructure.cs is accessible)

/// <summary>
/// Manages scene transitions between levels within the game session.
/// Handles additive loading/unloading, fade effects, and coordinates session state saving/loading.
/// Assumes it resides in the persistent GameRoot scene.
/// </summary>
public class TransitionManager : Singleton<TransitionManager> // Assuming you have a Singleton base class
{
    [Header("Fade Effect Settings")]
    [Tooltip("CanvasGroup used for the fade effect.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("Duration of the fade in/out animation in seconds.")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Scene Configuration")]
    [Tooltip("Name of the scene containing core game managers (this manager). Used for safety checks.")]
    [SerializeField] private string gameRootSceneName = "GameRoot"; // Important for checks

    /// <summary>
    /// Gets whether a fade transition is currently active.
    /// </summary>
    public bool IsTransitioning { get; private set; } = false;

    // Ensure fade canvas starts correctly
    protected override void Awake()
    {
        base.Awake(); // Call base Singleton Awake if it exists
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("Fade Canvas Group is not assigned in TransitionManager!");
            return;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    // --- Event Subscription for Event-Driven Transitions (Optional but Recommended) ---
    void OnEnable()
    {
        if (EventManager.Instance != null) EventManager.Instance.AddListener<TransitionRequestedEvent>(HandleTransitionRequest);
    }
    void OnDisable()
    {
        if (EventManager.Instance != null) EventManager.Instance.RemoveListener<TransitionRequestedEvent>(HandleTransitionRequest);
    }
    private void HandleTransitionRequest(TransitionRequestedEvent eventData)
    {
        if (eventData == null) return;
        Debug.Log($"TransitionManager received TransitionRequestedEvent: To '{eventData.TargetSceneName}', TargetID '{eventData.TargetTeleporterID}'");
        InitiateTeleport(eventData.TargetSceneName, eventData.TargetTeleporterID);
    }
    // --- End Event Subscription ---


    /// <summary>
    /// Public method to initiate the teleport sequence. Can be called directly or via event handler.
    /// </summary>
    /// <param name="toSceneName">The name of the target scene to load.</param>
    /// <param name="targetTeleporterID">The ID of the teleporter to spawn at in the target scene.</param>
    public void InitiateTeleport(string toSceneName, string targetTeleporterID)
    {
        if (IsTransitioning)
        {
            Debug.LogWarning("Teleport requested while already transitioning. Ignoring request.");
            return;
        }

        Scene sceneToUnload = SceneManager.GetActiveScene();

        if (string.IsNullOrEmpty(toSceneName)) { Debug.LogError("Teleport requested with an empty target scene name!"); return; }
        if (sceneToUnload.name == gameRootSceneName) { Debug.LogError($"Attempting to start transition FROM the GameRoot scene ('{gameRootSceneName}')? Aborting."); return; }

        Debug.Log($"Teleport requested. From (current active): '{sceneToUnload.name}' To: '{toSceneName}' Target ID: '{targetTeleporterID}'");
        StartCoroutine(TransformToScene(sceneToUnload, toSceneName, targetTeleporterID));
    }

    /// <summary>
    /// Coroutine that handles the actual scene loading, unloading, fading, and state management calls.
    /// </summary>
    private IEnumerator TransformToScene(Scene sceneToUnload, string toSceneName, string targetTeleporterID)
    {
        IsTransitioning = true;

        // --- Start Fade Out ---
        yield return Fade(1f);

        // --- Trigger Pre-Unload Event & Disable Player ---
        if (EventManager.Instance != null) EventManager.Instance.TriggerEvent(new BeforeSceneUnloadEvent());
        if (Player.Instance != null) Player.Instance.DisableCollision(); else Debug.LogWarning("Player.Instance not found during transition start.");

        // --- *** SAVE STATE of Scene Being Unloaded *** ---
        if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
        {
            GameSceneManager sceneManagerToSave = FindGameSceneManagerInScene(sceneToUnload);
            if (sceneManagerToSave != null)
            {
                SceneSaveData stateToSave = sceneManagerToSave.SaveCurrentState();
                if (SessionStateManager.Instance != null)
                {
                    SessionStateManager.Instance.RecordSceneState(sceneToUnload.name, stateToSave);
                } else { Debug.LogError("SessionStateManager not found! Cannot save scene state."); }
            } else { Debug.LogWarning($"GameSceneManager not found in scene '{sceneToUnload.name}'. Cannot save state.");}
        }
        // --- *** End Save State *** ---

        AsyncOperation loadOperation = null;
        AsyncOperation unloadOperation = null;
        Scene newScene = default; // Store the newly loaded scene reference

        // --- Load/Unload Scenes only if changing scenes ---
        if (sceneToUnload.name != toSceneName)
        {
            // 1. Load the new scene additively
            Debug.Log($"Coroutine: Loading '{toSceneName}' additively...");
            loadOperation = SceneManager.LoadSceneAsync(toSceneName, LoadSceneMode.Additive);
            if (loadOperation == null) { Debug.LogError($"Coroutine: Failed to start loading scene '{toSceneName}'."); yield return HandleTransitionError(); yield break; }
            while (!loadOperation.isDone) yield return null;
            Debug.Log($"Coroutine: Scene '{toSceneName}' loaded.");

            // 2. Set the newly loaded scene active
            newScene = SceneManager.GetSceneByName(toSceneName); // Get loaded scene reference
            if (newScene.IsValid())
            {
                // ***** THE FIX IS HERE *****
                SceneManager.SetActiveScene(newScene); // Actually set the scene active
                Debug.Log($"Coroutine: Set '{toSceneName}' as active scene.");
                // ***** END FIX *****
            }
            else
            {
                 Debug.LogError($"Coroutine: Failed to find scene '{toSceneName}' by name after loading! Cannot set active scene.");
                 yield return HandleTransitionError();
                 yield break;
            }

            // 3. Unload the previous active scene
            Debug.Log($"Coroutine: Attempting to unload previous scene '{sceneToUnload.name}'...");
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload); // Use the Scene object
                if (unloadOperation == null) { Debug.LogError($"Coroutine: Failed to start unloading scene '{sceneToUnload.name}'."); }
            }
            else { Debug.LogWarning($"Coroutine: Scene '{sceneToUnload.name}' was invalid or not loaded. Skipping unload."); }
            // Wait for unload if it started
            while (unloadOperation != null && !unloadOperation.isDone) yield return null;
            if (unloadOperation != null) Debug.Log($"Coroutine: Scene '{sceneToUnload.name}' unloaded.");

        }
        else
        {
            Debug.Log("Coroutine: Transitioning within the same scene (no scene load/unload).");
            newScene = sceneToUnload; // Target scene is the same as the one we started in
            if (newScene.IsValid()) SceneManager.SetActiveScene(newScene); // Ensure it's active just in case
        }

        // --- *** LOAD STATE for Newly Loaded/Activated Scene *** ---
        if (newScene.IsValid()) // Ensure we have a valid target scene
        {
             yield return null; // Wait a frame for objects in new scene to potentially Awake/Start
             GameSceneManager sceneManagerToLoad = FindGameSceneManagerInScene(newScene);
             if (sceneManagerToLoad != null)
             {
                  if (SessionStateManager.Instance != null && SessionStateManager.Instance.TryGetSceneState(newScene.name, out SceneSaveData loadedData))
                  {
                       Debug.Log($"Applying saved state to scene: {newScene.name}");
                       sceneManagerToLoad.LoadSaveData(loadedData);
                  } else {
                       Debug.Log($"No saved state found for scene: {newScene.name}. Initializing fresh.");
                  }
             } else { Debug.LogWarning($"GameSceneManager not found in newly loaded/activated scene '{newScene.name}'. Cannot load state.");}
        }
        // --- *** End Load State *** ---


        // --- Trigger Post-Unload Event & Move Player ---
        if (EventManager.Instance != null) EventManager.Instance.TriggerEvent(new AfterSceneUnloadEvent());
        yield return null;
        MovePlayerToTarget(targetTeleporterID);
        if (Player.Instance != null) Player.Instance.EnableCollision(); else Debug.LogWarning("Player.Instance not found during transition end.");

        // --- Start Fade In ---
        yield return Fade(0f);

        IsTransitioning = false;
        Debug.Log("Coroutine: Transition complete.");
    }

    // --- Helper function to find GameSceneManager within a specific scene ---
    private GameSceneManager FindGameSceneManagerInScene(Scene scene)
    {
         if (!scene.IsValid() || !scene.isLoaded) { Debug.LogWarning($"FindGameSceneManagerInScene called with invalid or unloaded scene: {scene.name}"); return null; }
         GameObject[] rootObjects = scene.GetRootGameObjects();
         foreach (GameObject rootObject in rootObjects)
         {
              GameSceneManager manager = rootObject.GetComponentInChildren<GameSceneManager>(true);
              if (manager != null) { return manager; }
         }
          Debug.LogWarning($"GameSceneManager component not found in the root objects of scene: {scene.name}");
         return null;
    }


    // --- Other methods remain the same: Fade, MovePlayerToTarget, FindTeleporterWithID, HandleTransitionError ---
    // ... (Paste the implementations for Fade, MovePlayerToTarget, FindTeleporterWithID, HandleTransitionError here from the previous version) ...

    /// <summary>
    /// Coroutine to handle the fade effect.
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true;
        float currentAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.1f);
    }

    /// <summary>
    /// Finds the target teleporter and moves the player to its spawn point.
    /// </summary>
    private void MovePlayerToTarget(string targetTeleporterID)
    {
        Debug.Log($"Attempting to move player to target teleporter ID: '{targetTeleporterID}'");
        ITeleportable targetTeleporter = FindTeleporterWithID(targetTeleporterID);
        if (targetTeleporter != null)
        {
            if (Player.Instance != null)
            {
                if (targetTeleporter.Spawnpoint != null)
                {
                    Player.Instance.transform.position = targetTeleporter.Spawnpoint.position;
                    Player.Instance.ZeroVelocity();
                    Debug.Log($"Player successfully moved to spawn point of '{targetTeleporterID}' at {targetTeleporter.Spawnpoint.position}");
                } else { Debug.LogError($"Target teleporter '{targetTeleporterID}' found, but its Spawnpoint transform is null!"); }
            } else { Debug.LogError("Player.Instance not found! Cannot move player."); }
        } else { Debug.LogError($"Target teleporter with ID '{targetTeleporterID}' not found in the newly loaded scene!"); }
    }

    /// <summary>
    /// Finds an ITeleportable component in the loaded scenes with the matching ID.
    /// </summary>
    private ITeleportable FindTeleporterWithID(string teleporterID)
    {
        var teleporters = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None); // Find all MonoBehaviours
        Debug.Log(teleporterID);
        Debug.Log(teleporters);
        foreach (var behaviour in teleporters)
        {
            if (behaviour is ITeleportable teleporter && teleporter.TeleportID == teleporterID)
            { return teleporter; }
        }
        return null;
    }

    /// <summary>
    /// Handles errors during the transition coroutine.
    /// </summary>
    private IEnumerator HandleTransitionError()
    {
         Debug.LogError("An error occurred during scene transition. Attempting to recover.");
         if (Player.Instance != null) Player.Instance.EnableCollision();
         yield return Fade(0f); // Fade back in
         IsTransitioning = false; // Allow trying again?
    }

}