using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraManager : MonoBehaviour
{
    public static MainCameraManager Instance;
    private Camera cam;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            cam = GetComponent<Camera>();
        }
        else
        {
            Destroy(gameObject); // 销毁重复的实例
        }
    }

    // 新增方法：切换摄像机渲染的层
    public void SetActiveLayer(string layerName)
    {
        // 将摄像机的cullingMask设置为只渲染指定层
        cam.cullingMask = LayerMask.GetMask(layerName);
    }
}
