using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private GameObject markerUI; // 标记 UI 的预制体

    #region Marker
    protected virtual void Start()
    {
        if (markerUI != null)
        {
            // 计算初始位置
            markerUI.SetActive(false); // 默认隐藏
        }
        else
        {
            Debug.LogWarning("Marker UI 预制体未设置！");
        }
    }

    // 显示标记 UI
    public void ShowMarker()
    {
        if (markerUI != null)
        {
            // 显示图标
            Debug.Log("显示图标");
            markerUI.SetActive(true);
        }
    }

    public void HideMarker()
    {
        if (markerUI != null)
        {
            markerUI.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (markerUI != null)
        {
            Destroy(markerUI); // 销毁 UI 防止内存泄漏
        }
    }
    #endregion

    public virtual void Interact()
    {
        Debug.Log("与 " + gameObject.name + " 交互");
    }
    
    public virtual bool UseItem(ItemData itemData)
    {
        // 默认不处理物品使用，返回false
        return false;
    }

    public virtual string GetDialogue()
    {
        return "按 E 与 " + gameObject.name + " 对话";
    }
}