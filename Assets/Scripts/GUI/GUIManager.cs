using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIManager : MonoBehaviour
{
    public static GUIManager gUIManager;

    void Awake()
    {
        if (gUIManager == null)
        {
            gUIManager = this;
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }
}
