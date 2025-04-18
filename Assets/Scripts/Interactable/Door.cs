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
    public string TargetTeleporterID => targetTeleporterID;
    public string TargetSceneName => targetSceneName;
    // private string currentSceneName; // REMOVE

    // override protected void Start() { base.Start(); /* currentSceneName = ... */ } // REMOVE

    public override void Interact()
    {
        base.Interact();
        InitiateTeleport(); // Call the new method
    }

    // Implementation of the interface method
    public void InitiateTeleport()
    {
        // OLD: TransitionManager.Instance.Teleport(TargetSceneName, TargetTeleportID);

        // NEW: Trigger an event via EventManager
        if (EventManager.Instance != null)
        {
            Debug.Log($"Triggering TransitionRequestedEvent to '{TargetSceneName}' (ID: '{TargetTeleporterID}') from {gameObject.name}");
            EventManager.Instance.TriggerEvent(new TransitionRequestedEvent(TargetSceneName, TargetTeleporterID));
        }
        else
        {
            Debug.LogError("EventManager instance not found! Cannot trigger transition event.");
        }
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
