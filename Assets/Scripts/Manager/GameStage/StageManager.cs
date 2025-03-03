using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager stageManager { get; private set; }

    [SerializeField] private StageData[] stages; // 在Inspector中配置所有阶段数据
    private int currentStageId = 0;              // 当前阶段ID

    void Awake()
    {
        if (stageManager == null)
        {
            stageManager = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // 初始化阶段
        SetStage(currentStageId);
    }

    // 切换阶段
    public void SetStage(int stageId)
    {
        if (stageId >= 0 && stageId < stages.Length)
        {
            currentStageId = stageId;
            StageChangeEvent stageEvent = new StageChangeEvent(currentStageId);
            EventManager.eventManager.TriggerEvent(stageEvent); // 触发阶段变化事件
            Debug.Log($"切换到阶段: {stages[currentStageId].stageName}");
        }
    }

    // 获取当前阶段数据（供其他系统查询）
    public StageData GetCurrentStageData()
    {
        return stages[currentStageId];
    }

    // 测试用：按键切换阶段
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)) SetStage(0);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetStage(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetStage(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetStage(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetStage(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetStage(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetStage(6);
    }
}