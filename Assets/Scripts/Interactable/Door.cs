using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : Interactable
{
    [SerializeField] private string doorID; // 本门的唯一标识符
    [SerializeField] private string targetDoorID; // 目标场景中对应门的标识符
    [SerializeField] private string targetSceneName; // 目标场景名称
    [SerializeField] private Transform spawnpoint; // 本门的生成点

    public string DoorID => doorID; // 提供公共访问属性
    public Transform Spawnpoint => spawnpoint;

    public override void Interact()
    {
        base.Interact();
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 调用 TransitionManager 的 Teleport 方法，传递 targetDoorID
        TransitionManager.Instance.Teleport(currentSceneName, targetSceneName, targetDoorID);
    }
    
    override protected void Start()
    {
        base.Start();
    }

    public override string GetDialogue()
    {
        return "这是一扇门，通向 " + targetSceneName;
    }
}