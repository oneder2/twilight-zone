using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData")]
public class StageData : ScriptableObject
{
    public int stageId;                // 阶段ID
    public string stageName;           // 阶段名称
    public float lightIntensity;       // 全局光强度
    public Color lightColor;           // 全局光颜色
    public float enemySpawnRate;       // 敌人生成速率
    public string dialogueMessage;     // 显示的对话内容
}