using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleInstance : MonoBehaviour
{
    public static SingleInstance instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 玩家对象不会被销毁
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }
}
