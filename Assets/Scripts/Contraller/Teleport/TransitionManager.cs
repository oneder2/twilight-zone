using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : Singleton<TransitionManager>
{
    // CanvasGroup for fade effect
    [Tooltip("CanvasGroup for fade effect")]
    public CanvasGroup fadeCanvasGroup;

    // Duration of fade animation in seconds
    [Tooltip("Fade animation duration")]
    public float fadeDuration;

    // True if fade animation is active
    public bool isFade;

    /// <summary>
    /// Teleports player from one scene to another with a target teleporter ID.
    /// </summary>
    public void Teleport(string from, string to, string entryTeleporterID)
    {
        StartCoroutine(TransformToScene(from, to, entryTeleporterID));
    }

    // Handles scene transition with fade and teleport
    private IEnumerator TransformToScene(string fromScene, string toScene, string targetTeleporterID)
    {
        yield return Fade(1); // Fade in
        EventManager.Instance.TriggerEvent(new BeforeSceneUnloadEvent());
        Player.Instance.DisableCollision();

        if (fromScene != toScene)
        {
            // Load new scene and unload old one
            yield return SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive);
            yield return SceneManager.UnloadSceneAsync(fromScene);
        }

        // Set new scene as active
        Scene newScene = SceneManager.GetSceneByName(toScene);
        SceneManager.SetActiveScene(newScene);

        EventManager.Instance.TriggerEvent(new AfterSceneUnloadEvent());
        ToTargetPoint(targetTeleporterID);
        Player.Instance.EnableCollision();

        yield return Fade(0); // Fade out
    }

    // Fades canvas alpha to target value
    private IEnumerator Fade(float targetAlpha)
    {
        isFade = true;
        fadeCanvasGroup.blocksRaycasts = true;

        float speed = Mathf.Abs(fadeCanvasGroup.alpha - targetAlpha) / fadeDuration;

        while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }

        fadeCanvasGroup.blocksRaycasts = false;
        isFade = false;
    }

    // Finds teleporter by ID
    private ITeleportable FindTeleporterWithID(string teleporterID)
    {
        GameObject[] teleporterObjects = GameObject.FindGameObjectsWithTag("Teleporter");
        foreach (GameObject obj in teleporterObjects)
        {
            ITeleportable teleporter = obj.GetComponent<ITeleportable>();
            if (teleporter != null && teleporter.TeleportID == teleporterID)
            {
                return teleporter;
            }
        }
        return null;
    }

    // Moves player to target teleporter's spawn point
    private void ToTargetPoint(string targetTeleporterID)
    {
        ITeleportable targetTeleporter = FindTeleporterWithID(targetTeleporterID);
        if (targetTeleporter != null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.transform.position = targetTeleporter.Spawnpoint.position;
                Debug.Log($"Player teleported to: {targetTeleporter} (ID: {targetTeleporterID})");
            }
            else
            {
                Debug.LogError("Player object not found!");
            }
        }
        else
        {
            Debug.LogError($"Target teleporter not found, ID: {targetTeleporterID}");
        }
    }
}