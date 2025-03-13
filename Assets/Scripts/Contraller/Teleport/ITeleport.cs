using UnityEngine;

public interface ITeleportable
{
    string TeleportID { get; }
    string TargetTeleportID { get; }
    string TargetSceneName { get; }
    Transform Spawnpoint { get; }
    void Teleport(string fromScene);
}