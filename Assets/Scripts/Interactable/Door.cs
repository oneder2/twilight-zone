using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : Interactable, ITeleportable
{
    [SerializeField] private string teleporterID; // 本门的唯一标识符
    [SerializeField] private string targetTeleporterID; // 目标场景中对应门的标识符
    [SerializeField] private string targetSceneName; // 目标场景名称
    [SerializeField] private Transform spawnpoint; // 本门的生成点


    public Transform Spawnpoint => spawnpoint;
    public string TeleportID => teleporterID;
    public string TargetTeleportID => targetTeleporterID;
    public string TargetSceneName => targetSceneName;
    private string currentSceneName;

    public void Teleport(string fromScene)
    {
        if (TransitionManager.Instance == null)
        {
            Debug.LogError("TransitionManager 未初始化！");
            return;
        }
        if (!TransitionManager.Instance.isFade)
        {
            TransitionManager.Instance.Teleport(fromScene, TargetSceneName, TargetTeleportID);
        }
    }


    public override void Interact()
    {
        base.Interact();
        Teleport(currentSceneName);
    }
    
    override protected void Start()
    {
        base.Start();
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    public override string GetDialogue()
    {
        return "这是一扇门，通向 " + targetSceneName;
    }
}
