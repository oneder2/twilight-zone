using UnityEngine;
using UnityEngine.SceneManagement;

public class LadderTeleporter : MonoBehaviour, ITeleportable
{
    [SerializeField] private string teleportID;
    [SerializeField] private string targetTeleportID;
    [SerializeField] private string fromSceneName;
    [SerializeField] private string targetSceneName;
    [SerializeField] private Transform spawnPoint;
    

    public string TeleportID => teleportID;
    public string TargetTeleportID => targetTeleportID;
    public string TargetSceneName => targetSceneName;
    public Transform Spawnpoint => spawnPoint;

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

    private void OnCollisionEnter2D(Collision2D other)
    {
        var player = other.collider.GetComponent<Player>();
        if (player != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Teleport(currentScene);
        }
    }
}
