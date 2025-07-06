using UnityEngine;

public interface ITeleportable
{
    string TeleportID { get; }
    string TargetTeleporterID { get; }
    string TargetSceneName { get; }
    Transform Spawnpoint { get; }
    void InitiateTeleport();
}