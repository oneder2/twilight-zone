using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public static DebugManager debuggManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            CompleteQuest(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CompleteQuest(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CompleteQuest(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            CompleteQuest(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            CompleteQuest(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            CompleteQuest(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            CompleteQuest(6);
        }
        
    }

    public void CompleteQuest(int stageId)
    {
        // 任务完成逻辑
        Debug.Log($"切换至游戏阶段：{stageId}");
        // 触发任务完成事件
        EventManager.eventManager.TriggerEvent(new StageChangeEvent(stageId));
    }
}
