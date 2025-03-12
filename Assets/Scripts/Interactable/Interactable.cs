using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private GameObject markerUIPrefab; // 标记 UI 的预制体
    [SerializeField] private Vector3 markerOffset = new Vector3(0, 0.5f, 0); // 默认偏移量，向上 0.5 单位
    private GameObject markerUI; // 实例化的标记 UI
    private float cameraZ; // 摄像机的 Z 轴位置

    void Start()
    {
        if (markerUIPrefab != null)
        {
            // 获取主摄像机的 Z 轴位置
            cameraZ = Camera.main.transform.position.z;

            // 计算初始位置
            Vector3 spawnPosition = transform.position + markerOffset;
            spawnPosition.z = cameraZ; // 设置 Z 轴与摄像机匹配
            markerUI = Instantiate(markerUIPrefab, spawnPosition, Quaternion.identity);
            markerUI.SetActive(false); // 默认隐藏
        }
        else
        {
            Debug.LogWarning("Marker UI 预制体未设置！");
        }
    }

    void Update()
    {
        if (markerUI != null)
        {
            // 持续更新标记 UI 的位置
            Vector3 newPosition = transform.position + markerOffset;
            newPosition.z = cameraZ; // 保持 Z 轴与摄像机一致
            markerUI.transform.position = newPosition;
        }
    }

    // 显示标记 UI
    public void ShowMarker()
    {
        if (markerUI != null)
        {
            markerUI.SetActive(true);
        }
    }

    // 隐藏标记 UI
    public void HideMarker()
    {
        if (markerUI != null)
        {
            markerUI.SetActive(false);
        }
    }

    // 清理实例化的标记 UI
    void OnDestroy()
    {
        if (markerUI != null)
        {
            Destroy(markerUI);
        }
    }

    public virtual void Interact()
    {
        Debug.Log("与 " + gameObject.name + " 交互");
    }

    public virtual string GetDialogue()
    {
        return "按 E 与 " + gameObject.name + " 对话";
    }
}