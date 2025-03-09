using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : Interactable
{
    [SerializeField] private SceneConnection connection;    // 场景连接对象
    [SerializeField] private string targetSceneName;        // 目标场景名称
    [SerializeField] private Transform spawnpoint;          // 目标场景的出生点（可选，用于调试）

    public override void Interact()
    {
        // 设置当前活动的场景连接
        SceneConnection.ActiveConnection = connection;

        // 获取当前场景名称
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 调用 TransitionManager 进行场景切换
        TransitionManager.Instance.Teleport(currentSceneName, targetSceneName);
    }

    public override string GetDialogue()
    {
        return "这是一扇门，通向 " + targetSceneName;
    }
}