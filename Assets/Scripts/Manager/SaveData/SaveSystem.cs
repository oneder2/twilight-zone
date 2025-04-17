using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class SaveSystem : Singleton<SaveSystem>
{
    private string saveFilePath;

    override protected void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/saveData.json";
    }

    private void Start()
    {
        // 订阅事件以处理场景切换
        EventManager.Instance.AddListener<BeforeSceneUnloadEvent>(OnBeforeSceneUnload);
        EventManager.Instance.AddListener<AfterSceneLoadEvent>(OnAfterSceneLoad); // 修改为 AfterSceneLoadEvent
        Debug.Log("SaveSystem subscribed to events");
    }

    private void OnDestroy()
    {
        // 移除事件监听器
        EventManager.Instance.RemoveListener<BeforeSceneUnloadEvent>(OnBeforeSceneUnload);
        EventManager.Instance.RemoveListener<AfterSceneLoadEvent>(OnAfterSceneLoad);

        // 在销毁时删除存档文件
        DeleteSaveFile();
        Debug.Log("SaveSystem destroyed, save file deleted if existed.");
    }

    /// <summary>
    /// 在场景卸载前触发，保留与 GameSceneManager 的交互，但不保存到文件
    /// </summary>
    private void OnBeforeSceneUnload(BeforeSceneUnloadEvent data)
    {
        Debug.Log("正在卸载");
        string sceneName = SceneManager.GetActiveScene().name;
        GameSceneManager sceneManager = FindAnyObjectByType<GameSceneManager>();
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            Debug.Log($"Processed state for scene: {sceneName} before unload (not saved to file)");
        }
        else
        {
            Debug.LogWarning($"No valid GameSceneManager found for scene: {sceneName}");
        }
    }

    /// <summary>
    /// 在新场景加载后触发，保留与 GameSceneManager 的交互，但不加载存档
    /// </summary>
    private void OnAfterSceneLoad(AfterSceneLoadEvent data)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        GameSceneManager sceneManager = FindAnyObjectByType<GameSceneManager>();
        if (sceneManager != null && sceneManager.sceneName == sceneName)
        {
            // 不从文件加载，直接跳过或重置状态
            sceneManager.LoadSaveData(null); // 传入 null 表示不恢复任何状态
            Debug.Log($"Scene {sceneName} loaded, no save data applied");
        }
        else
        {
            Debug.LogWarning($"No valid GameSceneManager found for scene: {sceneName}");
        }
    }

    /// <summary>
    /// 删除存档文件
    /// </summary>
    private void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted: " + saveFilePath);
        }
        else
        {
            Debug.LogWarning("Path does not exist");
        }
    }

    /// <summary>
    /// 手动触发销毁（例如返回主菜单时调用）
    /// </summary>
    public void DestroyAndCleanup()
    {
        Destroy(gameObject); // 触发 OnDestroy，自动删除存档
    }
}