using UnityEngine;
using System.Collections.Generic;

public class InteractableCheck : MonoBehaviour
{
    public float interactionRadius = 1.5f;
    public LayerMask interactableLayer;
    private Interactable nearbyInteractable;
    public bool hasInteractable = false;
    private List<Interactable> currentInteractables = new List<Interactable>();
    private List<Interactable> markedInteractables = new List<Interactable>();
    private CircleCollider2D triggerCollider;
    private InventoryUI inventoryUI; // 新增对InventoryUI的引用

    void Start()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;

        inventoryUI = FindAnyObjectByType<InventoryUI>(); // 获取InventoryUI
    }

    void Update()
    {
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

        UpdateNearbyInteractable();

        if (nearbyInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            ItemData selectedItem = inventoryUI.GetSelectedItemData();
            if (selectedItem != null)
            {
                if (nearbyInteractable.UseItem(selectedItem))
                {
                    Inventory.Instance.RemoveItem(selectedItem);
                    inventoryUI.ClearSelection(); // 使用后清除选中
                    inventoryUI.ToggleInventory(); // 更新UI
                }
            }
            else
            {
                nearbyInteractable.Interact();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        UpdateEnterList(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        UpdateEnterList(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        UpdateOutlist(other);
    }

    private void UpdateNearbyInteractable()
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
        }
        else
        {
            nearbyInteractable = null;
            hasInteractable = false;
        }
    }

    private void UpdateEnterList(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & interactableLayer) != 0)
        {
            Interactable interactable = other.GetComponent<Interactable>();
            if (interactable != null && !currentInteractables.Contains(interactable))
            {
                currentInteractables.Add(interactable);
                interactable.ShowMarker();
                markedInteractables.Add(interactable);
            }
        }
    }

    private void UpdateOutlist(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && currentInteractables.Contains(interactable))
        {
            currentInteractables.Remove(interactable);
            markedInteractables.Remove(interactable);
            interactable.HideMarker();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}