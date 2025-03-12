using UnityEngine;

public class IteractableCheck : MonoBehaviour
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
            nearbyInteractable.Interact();
    }

    void CheckForInteractables()
    {
        // 获取场景中所有带有 "Interactable" 标签的物体
        GameObject[] interactableObjects = GameObject.FindGameObjectsWithTag("Interactable");
        if (interactableObjects.Length > 0)
        {
            // 存在带有 "Interactable" 标签的物体，寻找最近的
            Interactable closestInteractable = null;
            float minDistance = float.MaxValue;
            // Debug.Log("检测到带有 Interactable 标签的物体数量：" + interactableObjects.Length);

            foreach (GameObject obj in interactableObjects)
            {
                // 获取 Interactable 组件
                Interactable interactable = obj.GetComponent<Interactable>();
                if (interactable != null)
                {
                    float distance = Vector2.Distance(transform.position, obj.transform.position);
                    if (distance < interactionRadius && distance < minDistance)
                    {
                        minDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            // 如果找到最近的可交互物体
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
                // Debug.Log("范围内无符合条件的可交互物体");
            }
        }
        else
        {
            nearbyInteractable = null;
            hasInteractable = false;
            // Debug.Log("场景中无 Interactable 标签的物体");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}