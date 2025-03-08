using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;
    private SaveData saveData;
    private string saveFilePath; // 移除直接初始化

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            saveFilePath = Application.persistentDataPath + "/saveData.json"; // 在 Awake 中初始化
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(saveData);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game saved to: " + saveFilePath);
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

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        GameSceneManager customSceneManager = FindFirstObjectByType<GameSceneManager>();
        if (customSceneManager != null)
        {
            SceneSaveData sceneSaveData = GetSceneSaveData(sceneName);
            customSceneManager.LoadSaveData(sceneSaveData);
            Debug.Log("Loaded save data for scene: " + sceneName);
        }
    }

    public void OnSceneUnloaded(Scene scene)
    {
        string sceneName = scene.name;
        GameSceneManager customSceneManager = FindFirstObjectByType<GameSceneManager>();
        if (customSceneManager != null)
        {
            SceneSaveData sceneSaveData = customSceneManager.SaveCurrentState();
            UpdateSceneSaveData(sceneName, sceneSaveData);
            Debug.Log("Saved state for scene: " + sceneName);
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