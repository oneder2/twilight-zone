using UnityEngine;
using System.Collections.Generic;

public class InteractableCheck : MonoBehaviour
{
    // 交互检测范围（触发器半径）
    public float interactionRadius = 1.5f;
    // 可交互对象的层
    public LayerMask interactableLayer;
    // 最近的可交互对象
    private Interactable nearbyInteractable;
    // 是否有可交互对象在范围内
    public bool hasInteractable = false;
    // 当前在触发器范围内的可交互对象列表
    private List<Interactable> currentInteractables = new List<Interactable>();
    // 当前显示标记的可交互对象列表
    private List<Interactable> markedInteractables = new List<Interactable>();

    // 缓存触发器组件
    private CircleCollider2D triggerCollider;

    void Start()
    {
        // 获取或添加 CircleCollider2D 作为触发器
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true; // 设置为触发器
        triggerCollider.radius = interactionRadius; // 设置触发器半径
    }

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
            currentInteractables.Clear();
            nearbyInteractable = null;
            hasInteractable = false;
            return;
        }

        // 更新最近的可交互对象
        UpdateNearbyInteractable();

        // 按下 'E' 键时与最近的对象交互
        if (nearbyInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            nearbyInteractable.Interact();
        }
    }

    // 当对象进入触发器范围时调用
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查层和组件
        if (((1 << other.gameObject.layer) & interactableLayer) != 0)
        {
            Interactable interactable = other.GetComponent<Interactable>();
            if (interactable != null && !currentInteractables.Contains(interactable))
            {
                currentInteractables.Add(interactable);
                interactable.ShowMarker();
                markedInteractables.Add(interactable);
                Debug.Log("进入范围: " + other.gameObject.name);
            }
        }
    }

    // 当对象离开触发器范围时调用
    void OnTriggerExit2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && currentInteractables.Contains(interactable))
        {
            currentInteractables.Remove(interactable);
            markedInteractables.Remove(interactable);
            interactable.HideMarker();
            Debug.Log("离开范围: " + other.gameObject.name);
        }
    }

    // 更新最近的可交互对象
    void UpdateNearbyInteractable()
    {
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
            Debug.Log("最近的可交互物体: " + closestInteractable.gameObject.name);
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