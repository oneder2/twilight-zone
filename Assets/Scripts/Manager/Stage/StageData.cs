// File: Scripts/Manager/GameStage/StageData.cs
using UnityEngine;

/// <summary>
/// ScriptableObject defining settings for a specific game stage.
/// 定义特定游戏阶段设置的 ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData")]
public class StageData : ScriptableObject
{
    [Header("基本信息 (Basic Info)")]
    public int stageId;                // 阶段ID (Stage ID)
    public string stageName;           // 阶段名称 (Stage Name)

    [Header("环境设置 (Environment Settings)")]
    public float lightIntensity = 1f;  // 全局光强度 (Global Light Intensity)
    public Color lightColor = Color.white; // 全局光颜色 (Global Light Color)
    public MusicTrack trackId = MusicTrack.None; // 背景音乐 (Background Music)

    [Header("敌人生成 (Enemy Spawning)")]
    [Tooltip("此阶段生成的敌人预制件 (Enemy prefab spawned in this stage)")]
    public GameObject enemyPrefab;     // 敌人预制件 (Enemy Prefab)

    [Tooltip("敌人生成的最小间隔时间（秒）。如果小于等于0，则不生成。(Minimum interval in seconds between enemy spawns. No spawning if <= 0)")]
    public float enemySpawnInterval = 5f; // 敌人生成间隔 (Enemy Spawn Interval)

    [Tooltip("场景中允许同时存在的最大敌人数量。(Maximum number of enemies allowed in the scene at the same time)")]
    public int maxActiveEnemies = 3;   // 最大活跃敌人数量 (Max Active Enemies)

    [Tooltip("此阶段生成敌人的基础移动速度。(Base movement speed for enemies spawned in this stage)")]
    public float enemyBaseSpeed = 2f;    // 敌人基础速度 (Enemy Base Speed)

    // [Header("陷阱生成 (Trap Spawning) - 未来扩展 (Future Expansion)")]
    // public GameObject trapPrefab;
    // public float trapSpawnInterval = 10f;
    // public int maxActiveTraps = 5;
    // public float trapSizeModifier = 1f;

    [Header("对话/提示 (Dialogue/Hints)")]
    [TextArea]
    public string dialogueMessage;     // 显示的对话内容 (Dialogue message to display)
}