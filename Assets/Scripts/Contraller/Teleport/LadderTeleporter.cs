using UnityEngine;
using UnityEngine.SceneManagement;

public class LadderTeleporter : MonoBehaviour, ITeleportable
{
    // current teleporter id
    [SerializeField] private string teleportID;
    // target teleporter id
    [SerializeField] private string targetTeleportID;
    // the scene begin to teleport
    [SerializeField] private string fromSceneName;
    // the scene teleport to
    [SerializeField] private string targetSceneName;
    // the spawn point of this teleporter, 
    // if any player is teleported from another teleporter to this teleporter
    [SerializeField] private Transform spawnPoint;
    

    // Teleporter ID inherited from ITeleport
    public string TeleportID => teleportID;
    // Target teleporter ID inherited from ITeleport
    public string TargetTeleportID => targetTeleportID;
    // Target scene name inherited from ITeleport
    public string TargetSceneName => targetSceneName;
    // Spawn point game object inherited from ITeleport
    public Transform Spawnpoint => spawnPoint;
    private string currentSceneName;
    


    // Teleport method inherited from ITeleport
    public void Teleport(string fromScene)
    {
        if (TransitionManager.Instance == null)
        {
            Debug.LogError("SideTransitionManager 未初始化！");
            return;
        }

        if (!TransitionManager.Instance.isFade)
        {
            TransitionManager.Instance.Teleport(fromScene, TargetSceneName, TargetTeleportID);
        }
    }

    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    // When Collision happens with current game object
    private void OnCollisionEnter2D(Collision2D other)
    {
        var player = other.collider.GetComponent<Player>();
        if (player != null)
        {
            Teleport(currentSceneName);
        }
    }
}
