using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public GameSceneManager Instance;
    public MainCameraManager mainCameraManager;
    private string currentScene;

    public string[] sceneNames = {
        "Basement", 
        "Outside", 
        "Floor1", 
        "Floor2", 
        "Floor3", 
        "RoofTop"
        };

    private void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 加载所有后台场景
        foreach (string sceneName in sceneNames)
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
        SetActiveScene(sceneNames[0]);
    }

    public void SetActiveScene(string sceneName)
    {
        if (!System.Array.Exists(sceneNames, name => name == sceneName))
        {
            Debug.LogError("场景名称无效：" + sceneName);
            return;
        }

        currentScene = sceneName;

        // 切换单例摄像机的渲染层
        mainCameraManager.SetActiveLayer(sceneName);
        Debug.Log($"当前场景切换至：{sceneName}");
    }

    // 获取当前场景名称（用于调试或逻辑）
    public string GetCurrentScene()
    {
        return currentScene;
    }
}
