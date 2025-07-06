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
        
    }
}