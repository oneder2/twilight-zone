using UnityEngine;


public class LadderTeleporter : MonoBehaviour, ITeleportable
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
    

    public Transform Spawnpoint => spawnpoint;
    public string TeleportID => teleporterID;
    public string TargetTeleporterID => targetTeleporterID;
    public string TargetSceneName => targetSceneName;
    

    // Implementation of the interface method
    public void InitiateTeleport()
    {
        Debug.Log($"本传送ID：{TeleportID}");
        Debug.Log($"目标传送ID：{TargetTeleporterID}");
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


    // When Collision happens with current game object
    private void OnCollisionEnter2D(Collision2D other)
    {
        var player = other.collider.GetComponent<Player>();
        if (player != null)
        {
            InitiateTeleport(); // Call the new method
        }
    }
}
