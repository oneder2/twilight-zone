using UnityEngine;
using System.Collections.Generic;

public class InteractableCheck : MonoBehaviour
{
    // 交互检测范围
    public float interactionRadius = 1.5f;
    // 可交互对象的层
    public LayerMask interactableLayer;
    // 最近的可交互对象
    private Interactable nearbyInteractable;
    // 是否有可交互对象在范围内
    public bool hasInteractable = false;
    // 当前显示标记的可交互对象列表
    private List<Interactable> markedInteractables = new List<Interactable>();

    void Update()
    {
        // 如果在对话中，隐藏所有标记并清空状态
        if (GameManager.Instance.isInDialogue)
        {
            foreach (Interactable interactable in markedInteractables)
            {
                interactable.HideMarker();
            }
            markedInteractables.Clear();
            nearbyInteractable = null;
            hasInteractable = false;
            return;
        }

        // 检测并更新可交互对象
        CheckForInteractables();

        // 按下 'E' 键时与最近的对象交互
        if (nearbyInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            nearbyInteractable.Interact();
        }
    }

    void CheckForInteractables()
    {
        // 使用圆形范围检测所有可交互对象
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);
        List<Interactable> currentInteractables = new List<Interactable>();

        // 收集当前范围内的所有可交互对象
        foreach (Collider2D collider in colliders)
        {
            Interactable interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                currentInteractables.Add(interactable);
            }
        }

        // 使用 HashSet 优化查找
        HashSet<Interactable> currentSet = new HashSet<Interactable>(currentInteractables);

        // 显示新进入范围的对象的标记
        foreach (Interactable interactable in currentInteractables)
        {
            if (!markedInteractables.Contains(interactable))
            {
                interactable.ShowMarker();
                markedInteractables.Add(interactable);
            }
        }

        // 隐藏离开范围的对象的标记
        for (int i = markedInteractables.Count - 1; i >= 0; i--)
        {
            Interactable interactable = markedInteractables[i];
            if (!currentSet.Contains(interactable))
            {
                interactable.HideMarker();
                markedInteractables.RemoveAt(i);
            }
        }

        // 找到最近的交互对象
        if (currentInteractables.Count > 0)
        {
            Interactable closestInteractable = currentInteractables[0];
            float minDistance = Vector2.Distance(transform.position, closestInteractable.transform.position);
            for (int i = 1; i < currentInteractables.Count; i++)
            {
                float distance = Vector2.Distance(transform.position, currentInteractables[i].transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestInteractable = currentInteractables[i];
                }
            }
            nearbyInteractable = closestInteractable;
            hasInteractable = true;
            Debug.Log("检测到可交互物体：" + closestInteractable.gameObject.name);
        }
        else
        {
            nearbyInteractable = null;
            hasInteractable = false;
        }
    }

    // 在编辑器中绘制交互范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}