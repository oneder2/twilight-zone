using UnityEngine;
using UnityEngine.UI;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private Text promptText;
    [SerializeField] private float interactRange = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    void Start()
    {
        EventManager.Instance.AddListener<DialogueStateChangedEvent>(OnDialogueStateChanged);
    }

    private void OnDialogueStateChanged(DialogueStateChangedEvent data)
    {
        if (data.IsInDialogue)
        {
            promptText.text = "";
        }
    }

    void Update()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayer);
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

        promptText.text = closestInteractable != null ? closestInteractable.GetDialogue() : "";
    }
}