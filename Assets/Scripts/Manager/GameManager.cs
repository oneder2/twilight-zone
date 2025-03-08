using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public GUIManager guiManager;
    public EventManager eventManager;
    public GameSceneManager sceneManager; // 修正为 GameSceneManager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 添加 DontDestroyOnLoad，与 SaveSystem 一致
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SaveSystem.Instance != null)
        {
            SceneManager.sceneLoaded += SaveSystem.Instance.OnSceneLoaded;
            SceneManager.sceneUnloaded += SaveSystem.Instance.OnSceneUnloaded;
        }
        else
        {
            Debug.LogError("SaveSystem.Instance is null in GameManager.Start()");
        }
    }

    private void OnDestroy()
    {
        if (SaveSystem.Instance != null)
        {
            SceneManager.sceneLoaded -= SaveSystem.Instance.OnSceneLoaded;
            SceneManager.sceneUnloaded -= SaveSystem.Instance.OnSceneUnloaded;
        }
        else
        {
            Debug.LogWarning("SaveSystem.Instance is null in GameManager.OnDestroy()");
        }
    }
}