using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;
    private SaveData saveData;
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            saveFilePath = Application.persistentDataPath + "/saveData.json";
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EventManager.Instance.AddListener<BeforeSceneUnloadEvent>(OnBeforeSceneUnload);
        EventManager.Instance.AddListener<AfterSceneUnloadEvent>(OnAfterSceneUnload); // 确保是 AfterSceneLoadEvent
        Debug.Log("SaveSystem subscribed to events");
    }

    private void OnDestroy()
    {
        EventManager.Instance.RemoveListener<BeforeSceneUnloadEvent>(OnBeforeSceneUnload);
        EventManager.Instance.RemoveListener<AfterSceneUnloadEvent>(OnAfterSceneUnload);
    }

    /// <summary>
    /// 在场景卸载前保存当前场景的状态
    /// </summary>
    private void OnBeforeSceneUnload(BeforeSceneUnloadEvent data)
    {
        Debug.Log("正在卸载");
        string sceneName = SceneManager.GetActiveScene().name; // 获取当前活动场景
        GameSceneManager sceneManager = FindFirstObjectByType<GameSceneManager>();
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            SceneSaveData sceneSaveData = sceneManager.SaveCurrentState();
            UpdateSceneSaveData(sceneName, sceneSaveData);
            Debug.Log($"Saved state for scene: {sceneName} before unload");
        }
        else
        {
            Debug.LogWarning($"No valid GameSceneManager found for scene: {sceneName}");
        }
    }

    /// <summary>
    /// 在新场景加载后恢复状态
    /// </summary>
    private void OnAfterSceneUnload(AfterSceneUnloadEvent data)
    {
        string sceneName = SceneManager.GetActiveScene().name; // 获取新加载的活动场景
        GameSceneManager sceneManager = FindFirstObjectByType<GameSceneManager>();
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            SceneSaveData sceneSaveData = GetSceneSaveData(sceneName);
            sceneManager.LoadSaveData(sceneSaveData);
            Debug.Log($"Loaded save data for scene: {sceneName} after load");
        }
        else
        {
            Debug.LogWarning($"No valid GameSceneManager found for scene: {sceneName}");
        }
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved to: " + saveFilePath + " with content: " + json);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            saveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Game loaded from: " + saveFilePath);
        }
        else
        {
            saveData = new SaveData { sceneSaveData = new List<SceneDataPair>() };
            Debug.Log("No save file found, created new save data.");
        }
    }

    private SceneSaveData GetSceneSaveData(string sceneName)
    {
        foreach (var pair in saveData.sceneSaveData)
        {
            if (pair.sceneName == sceneName)
            {
                return pair.sceneData;
            }
        }
        return new SceneSaveData { itemsState = new List<ItemStatePair>() };
    }

    private void UpdateSceneSaveData(string sceneName, SceneSaveData sceneSaveData)
    {
        for (int i = 0; i < saveData.sceneSaveData.Count; i++)
        {
            if (saveData.sceneSaveData[i].sceneName == sceneName)
            {
                saveData.sceneSaveData[i].sceneData = sceneSaveData;
                SaveGame();
                return;
            }
        }
        saveData.sceneSaveData.Add(new SceneDataPair { sceneName = sceneName, sceneData = sceneSaveData });
        SaveGame();
    }
}