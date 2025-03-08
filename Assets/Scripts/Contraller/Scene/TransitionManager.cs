using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : Singleton<TransitionManager>
{
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration;
    private bool isFade;

    public void Teleport(string from, string to)
    {
        StartCoroutine(TransformToScene(from, to));
    }

    private IEnumerator TransformToScene(string from, string to)
    {
        yield return Fade(1);
        EventHandler.CallBeforeSceneUnloadEvent();

        yield return SceneManager.LoadSceneAsync(to, LoadSceneMode.Additive);
        yield return SceneManager.UnloadSceneAsync(from);
        
        Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        SceneManager.SetActiveScene(newScene);
        
        EventHandler.CallAfterSceneUnloadEvent();
        yield return Fade(0);
    }

    // Graduade alpha change
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

}