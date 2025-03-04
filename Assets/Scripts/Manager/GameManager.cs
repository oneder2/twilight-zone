using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GUIManager guiManager;
    public StageManager stageManager;
    public DebugManager debugManager;
    public EventManager eventManager;
    public SceneManager sceneManager;
    public EventSystem eventSystem;
    public DialogueGUI dialogueGUI;
    

    private void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：跨场景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        
    }

}
