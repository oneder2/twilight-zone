using UnityEngine;

/// <summary>
/// Deactivates the GameObject this script is attached to when the game
/// transitions into a specific target GameStatus.
/// Listens for the GameStatusChangedEvent broadcast by the EventManager.
/// </summary>
public class DeactivateOnStatus : MonoBehaviour
{
    [Tooltip("The GameStatus that will trigger the deactivation of this GameObject.")]
    [SerializeField] private GameStatus deactivateOnStatus = GameStatus.GameOver; // Example default, set in Inspector

    // --- Unity Lifecycle Methods for Event Subscription ---

    void OnEnable()
    {
        // Subscribe to the game status event when enabled
        if (EventManager.Instance != null)
        {
            EventManager.Instance.AddListener<GameStatusChangedEvent>(HandleGameStatusChange);
            Debug.Log($"{gameObject.name}: Subscribed to GameStatusChangedEvent for deactivation on {deactivateOnStatus}.");
        }
        else
        {
            Debug.LogError("EventManager.Instance is null on Enable. Cannot subscribe to events.");
        }

        // Optional: Check initial state immediately in case it should already be inactive
        // if (GameRunManager.Instance != null)
        // {
        //     if (GameRunManager.Instance.CurrentStatus == deactivateOnStatus)
        //     {
        //         Debug.Log($"{gameObject.name}: Deactivating immediately on Enable because current status is {deactivateOnStatus}.");
        //         gameObject.SetActive(false);
        //         // Note: OnDisable will be called immediately after SetActive(false),
        //         // ensuring the listener is unsubscribed correctly.
        //     }
        // }
    }

    void OnDisable()
    {
        // Unsubscribe from the event when disabled or destroyed
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<GameStatusChangedEvent>(HandleGameStatusChange);
            // Debug.Log($"{gameObject.name}: Unsubscribed from GameStatusChangedEvent.");
        }
    }

    // --- Event Handler Method ---

    /// <summary>
    /// Handles the GameStatusChangedEvent. Checks if the new status matches the target status.
    /// </summary>
    /// <param name="eventData">The event data containing the new and previous status.</param>
    private void HandleGameStatusChange(GameStatusChangedEvent eventData)
    {
        // Check if the new game status is the one we want to deactivate on
        if (eventData.NewStatus == deactivateOnStatus)
        {
            Debug.Log($"Deactivating self ({gameObject.name}) due to game status change to {deactivateOnStatus}.");
            // Deactivate the GameObject this script is attached to
            Destroy(gameObject);
            // Note: Deactivating the GameObject will automatically call OnDisable,
            // which handles unsubscribing from the event.
        }
    }
}
