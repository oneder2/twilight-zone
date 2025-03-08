using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private StageData[] stages; // 在Inspector中配置所有阶段数据
    private int currentStageId = 0;              // 当前阶段ID

    void Start()
    {
        // 监听阶段变化
        EventManager.Instance.AddListener<StageChangeEvent>(OnStageChanged);
    }

    void OnStageChanged(StageChangeEvent stageEvent)
    {
        SetStage(stageEvent.StageId);
    }

    // 切换阶段
    public void SetStage(int stageId)
    {
        if (stageId >= 0 && stageId < stages.Length)
        {
            currentStageId = stageId;
            StageChangeEvent stageEvent = new StageChangeEvent(currentStageId);
            Debug.Log($"切换到阶段: {stages[currentStageId].stageName}");
        }
    }

    // 获取当前阶段数据（供其他系统查询）
    public StageData GetCurrentStageData()
    {
        return stages[currentStageId];
    }
}