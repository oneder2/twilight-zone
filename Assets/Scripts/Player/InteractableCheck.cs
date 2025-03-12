using UnityEngine;

public class InteractableCheck : MonoBehaviour
{
    public float interactionRadius = 1.5f;    // 检测范围半径
    public LayerMask interactableLayer;       // 可交互物体的层
    private Interactable nearbyInteractable;  // 最近的可交互物体
    public bool hasInteractable = false;      // 是否有可交互物体

    void Update()
    {
        // 对话期间禁用交互检测
        if (GameManager.instance.isInDialogue)
        {
            nearbyInteractable = null;
            hasInteractable = false;
            return;
        }

        CheckForInteractables();

        // 当有可交互物体且按下 'E' 键时，触发交互
        if (nearbyInteractable != null && Input.GetKeyDown(KeyCode.E))
            nearbyInteractable.Interact();
    }

    void CheckForInteractables()
    {
        // 使用 Physics2D.OverlapCircleAll 检测范围内可交互物体
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);
        Interactable closestInteractable = null;
        float minDistance = float.MaxValue;

        foreach (Collider2D collider in colliders)
        {
            Interactable interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        if (closestInteractable != null)
        {
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}