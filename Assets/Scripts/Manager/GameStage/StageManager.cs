using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private StageData[] stages; // 在Inspector中配置所有阶段数据
    private int currentStageId = 0;              // 当前阶段ID

    void Start()
    {
        // TODO Stage setting, adjust in later coding
        EventManager.Instance.RegisterTimeEvent("切换到阶段1", 60, new StageChangeEvent(1));
        EventManager.Instance.RegisterTimeEvent("切换到阶段2", 120, new StageChangeEvent(2));
        EventManager.Instance.RegisterTimeEvent("切换到阶段3", 180, new StageChangeEvent(3));
        EventManager.Instance.RegisterTimeEvent("切换到阶段4", 240, new StageChangeEvent(4));
        EventManager.Instance.RegisterTimeEvent("切换到阶段5", 300, new StageChangeEvent(5));
        EventManager.Instance.RegisterTimeEvent("切换到最终阶段", 360, new StageChangeEvent(6));

        // 监听阶段变化事件
        EventManager.Instance.AddListener<StageChangeEvent>(OnStageChanged);
        // 初始化第一阶段
        ApplyStageSettings();
    }

    void OnDestroy()
    {
        EventManager.Instance.RemoveListener<StageChangeEvent>(OnStageChanged);
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
            ApplyStageSettings();
            Debug.Log($"切换到阶段: {stages[currentStageId].stageName}");
        }
    }

    // 应用当前阶段的设置
    private void ApplyStageSettings()
    {
        StageData currentStage = stages[currentStageId];
        // 更新光照
        // 更新背景音乐
        UpdateBackgroundMusic(currentStage.trackId);
        // 更新背景图像
    }

    // 更新背景音乐
    private void UpdateBackgroundMusic(MusicTrack trackId)
    {
        AudioManager.Instance.PlayMusic(trackId);
    }

    // 获取当前阶段数据
    public StageData GetCurrentStageData()
    {
        return stages[currentStageId];
    }
}