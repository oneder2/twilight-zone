using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;

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
            DontDestroyOnLoad(gameObject);
            saveFilePath = Application.persistentDataPath + "/saveData.json";
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        GameSceneManager sceneManager = FindAnyObjectByType<GameSceneManager>();
        Debug.Log((sceneManager.sceneName, sceneName));
        Debug.Log((sceneManager != null, sceneManager.sceneName == sceneName));
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            SceneSaveData sceneSaveData = GetSceneSaveData(sceneName);
            sceneManager.LoadSaveData(sceneSaveData);
            Debug.Log($"Loaded save data for scene: {sceneName}");
        }
        else
        {
            Debug.LogWarning($"No valid GameSceneManager found for scene: {sceneName}");
        }
    }

    public void OnSceneUnloaded(Scene scene)
    {
        string sceneName = scene.name;
        GameSceneManager sceneManager = FindFirstObjectByType<GameSceneManager>();
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            SceneSaveData sceneSaveData = sceneManager.SaveCurrentState();
            UpdateSceneSaveData(sceneName, sceneSaveData);
            Debug.Log($"Saved state for scene: {sceneName}");
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