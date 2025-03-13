using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TransitionManager : Singleton<TransitionManager>
{
    // Fade effect canvas
    public CanvasGroup fadeCanvasGroup;
    // fade animation duration
    public float fadeDuration;
    // if fafing animation is playing
    public bool isFade;

    // Teleport:
    // summary: This method will teleport player from current scene to a appointed target scene
    // string from: The original scene name teleport at
    // string to: The target scene name teleport aims
    // string entryTeleporterID: The target teleporter ID
    public void Teleport(string from, string to, string entryTeleporterID)
    {
        StartCoroutine(TransformToScene(from, to, entryTeleporterID));
    }


    private IEnumerator TransformToScene(string fromScene, string toScene, string targetTeleporterID)
    {
        // 渐变进入
        yield return Fade(1);
        EventHandler.CallBeforeSceneUnloadEvent();
        Player.Instance.DisableCollision();
        
        if (fromScene != toScene)
        {
            // 异步加载新场景并卸载旧场景
            yield return SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive);
            yield return SceneManager.UnloadSceneAsync(fromScene);
        }

        // 设置新场景为活动场景
        Scene newScene = SceneManager.GetSceneByName(toScene);
        SceneManager.SetActiveScene(newScene);

        EventHandler.CallAfterSceneUnloadEvent();
        
        // Teleport player to target point of target teleporter
        ToTargetPoint(targetTeleporterID);
        Player.Instance.EnableCollision();

        // 渐变退出
        yield return Fade(0);
        EventHandler.CallAfterSceneLoadEvent();
    }

    // Gradual alpha change
    private IEnumerator Fade(float targetAlpha)
    {
        isFade = true;

        fadeCanvasGroup.blocksRaycasts = true;

        float speed = Math.Abs(fadeCanvasGroup.alpha - targetAlpha) / fadeDuration;

        while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }

        fadeCanvasGroup.blocksRaycasts = false;

        isFade = false;
    }

    // Find teleporter object with ID
    private ITeleportable FindTeleporterWithID(string teleporterID)
    {
        // Find every GameObjects with tag "Teleporter"
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

    private void ToTargetPoint(string targetTeleporterID)
    {
    // 查找目标门并传送玩家
        ITeleportable targetTeleporter = FindTeleporterWithID(targetTeleporterID);
        if (targetTeleporter != null)
        {
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.transform.position = targetTeleporter.Spawnpoint.position;
                Debug.Log($"玩家传送到: {targetTeleporter} (ID: {targetTeleporterID})");
            }
            else
            {
                Debug.LogError("未找到玩家对象！");
            }
        }
        else
        {
            Debug.LogError($"未找到目标门，ID: {targetTeleporterID}");
        }
    }
}