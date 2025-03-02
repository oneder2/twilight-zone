using UnityEngine;

public class ItemCheck : MonoBehaviour
{
    public float interactionRadius = 1.5f;    // 检测范围半径
    public LayerMask interactableLayer;       // 可交互物体的层
    private Interactable nearbyInteractable;  // 最近的可交互物体
    public bool hasInteractable = false;      // 是否有可交互物体（更贴切的命名）

    void Update()
    {
        CheckForInteractables();

        // 当有可交互物体且按下 'E' 键时，触发交互
        if (nearbyInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            nearbyInteractable.Interact();
        }
    }

    void CheckForInteractables()
    {
        // 检测范围内的所有碰撞体
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);
        if (hits.Length > 0)
        {
            // 寻找最近的可交互物体
            Collider2D closest = null;
            float minDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                Interactable interactable = hit.GetComponent<Interactable>();
                if (interactable != null)
                {
                    float distance = Vector2.Distance(transform.position, hit.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = hit;
                    }
                }
            }

            if (closest != null)
            {
                nearbyInteractable = closest.GetComponent<Interactable>();
                hasInteractable = true;
                Debug.Log("检测到可交互物体：" + closest.gameObject.name);
            }
            else
            {
                nearbyInteractable = null;
                hasInteractable = false;
            }
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