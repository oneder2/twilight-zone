using UnityEngine;


public class Door : Interactable, ITeleportable
{
    // current teleporter id
    [SerializeField] private string teleporterID;
    // target teleporter id
    [SerializeField] private string targetTeleporterID;
    // the scene teleport to
    [SerializeField] private string targetSceneName;
    // the spawn point of this teleporter, 
    // if any player is teleported from another teleporter to this teleporter
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private bool available = true;


    public Transform Spawnpoint => spawnpoint;
    public string TeleportID => teleporterID;
    public string TargetTeleporterID => targetTeleporterID;
    public string TargetSceneName => targetSceneName;


    public override void Interact()
    {
        base.Interact();
        InitiateTeleport(); // Call the new method
    }

    // Implementation of the interface method
    public void InitiateTeleport()
    {
        if (EventManager.Instance != null)
        {
            Debug.Log($"Triggering TransitionRequestedEvent to '{TargetSceneName}' (ID: '{TargetTeleporterID}') from {gameObject.name}");
            if (available)
            {
                EventManager.Instance.TriggerEvent(new TransitionRequestedEvent(TargetSceneName, TargetTeleporterID));
            } else
            {
                DialogueManager.Instance.ShowBlockingDialogue(GetDialogue());
            }

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
        return $"The door to {targetSceneName} is unaccesable";
    }
}
