using UnityEditor.SceneManagement;
using UnityEngine;

public class TimeEventsManager : MonoBehaviour
{
    public static TimeEventsManager Instance { get; private set; }
    

    void Awake()
    {
        EventManager.Instance.RegisterTimeEvent("切换到阶段4", 5, new StageChangeEvent(4));
    }
}