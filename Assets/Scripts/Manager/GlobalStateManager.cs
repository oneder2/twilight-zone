using UnityEngine;
using System.Collections.Generic;

public class GlobalStateManager : MonoBehaviour
{
    public static GlobalStateManager Instance { get; private set; }
    private Dictionary<string, Dictionary<string, object>> sceneStates = new Dictionary<string, Dictionary<string, object>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保管理器不被销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 保存状态
    public void SaveSceneState(string sceneName, string objectId, object state)
    {
        if (!sceneStates.ContainsKey(sceneName))
        {
            sceneStates[sceneName] = new Dictionary<string, object>();
        }
        sceneStates[sceneName][objectId] = state;
    }

    // 获取状态
    public object GetSceneState(string sceneName, string objectId)
    {
        if (sceneStates.ContainsKey(sceneName) && sceneStates[sceneName].ContainsKey(objectId))
        {
            return sceneStates[sceneName][objectId];
        }
        return null;
    }
}