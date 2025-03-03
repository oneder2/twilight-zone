using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStageManager : MonoBehaviour
{
    public static GameStageManager gameStageManager;

    void Awake()
    {
        if (gameStageManager == null)
        {
            gameStageManager = this;
            DontDestroyOnLoad(gameObject); // 玩家对象不会被销毁
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }

    private void OnEnable()
    {
        // 组件启用时注册监听器
        EventManager.eventManager.AddListener<StageChangeEvent>(OnQuestCompleted);
    }

    // private void OnDisable()
    // {
    //     // 组件禁用时移除监听器
    //     EventManager.eventManager.RemoveListener<StageChangeEvent>(OnQuestCompleted);
    // }

    private void OnQuestCompleted(StageChangeEvent eventData)
    {
        switch (eventData.StageId)
        {
            case 0:
                // 当前阶段为游戏初始阶段
                Debug.Log("死亡人数:0,没有人被杀死");
                break;
            case 1:
                // 杀死1人
                Debug.Log("死亡人数:1,小信徒被杀死");
                break;
            case 2:
                // 杀死2人
                Debug.Log("死亡人数:2,小信徒,暗恋者妹妹被杀死");
                break;
            case 3:
                // 杀死3人
                Debug.Log("死亡人数:3,小信徒,暗恋者妹妹,挚友被杀死");
                break;
            case 4:
                // 杀死4人
                Debug.Log("死亡人数:4,小信徒,暗恋者妹妹,挚友,暗恋者被杀死");
                break;
            case 5:
                // 杀死5人
                Debug.Log("死亡人数:5,小信徒,暗恋者妹妹,挚友,暗恋者,恩师被杀死");
                break;
            case 6:
                // 杀死6人（已自杀）
                Debug.Log("死亡人数:6,小信徒,暗恋者妹妹,挚友,暗恋者,恩师被杀死，且已经自杀");
                break;
        }
    }
}
