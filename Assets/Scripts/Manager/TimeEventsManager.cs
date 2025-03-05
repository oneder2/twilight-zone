using UnityEditor.SceneManagement;
using UnityEngine;

public class TimeEventsManager : MonoBehaviour
{
    public static TimeEventsManager Instance { get; private set; }
    
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

    private void Start()
    {
        EventManager.Instance.RegisterTimeEvent("切换到阶段1", 60, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到阶段2", 120, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到阶段3", 180, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到阶段4", 240, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到阶段5", 300, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到最终阶段", 360, new StageChangeEvent(4));
    }
}