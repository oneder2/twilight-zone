using UnityEngine;
using Cinemachine; // Assuming use of Cinemachine
using System.Collections.Generic;

/// <summary>
/// Manages Cinemachine Virtual Cameras and camera focus during gameplay and cutscenes.
/// Assumed to be a Singleton, potentially persistent (DontDestroyOnLoad).
/// </summary>
public class CameraManager : Singleton<CameraManager> // Use your Singleton base
{
    [Header("Default Camera")]
    [Tooltip("The virtual camera that normally follows the player.")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;

    // --- Optional: References to specific cutscene cameras ---
    // You might assign these in the Inspector or find them by name/tag
    // [SerializeField] private CinemachineVirtualCamera npcCloseUpCamera;
    // [SerializeField] private CinemachineVirtualCamera wideShotCamera;

    // Store the player transform reference
    private Transform playerTransform;
    private CinemachineBrain cinemachineBrain; // Reference to the brain for blend times

    protected override void Awake()
    {
        base.Awake();
        // Ensure DontDestroyOnLoad if this is persistent
        // DontDestroyOnLoad(gameObject);

        cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>(); // Find brain on main camera
        if (cinemachineBrain == null) Debug.LogError("Cinemachine Brain not found on Main Camera!", this);

        if (playerFollowCamera == null) Debug.LogError("Player Follow Camera is not assigned in CameraManager!", this);
    }

    void Start()
    {
        // Try to find Player immediately if available
        if (Player.Instance != null) SetPlayerTransform(Player.Instance.transform);
        // If not, GameRunManager or Player itself should call SetPlayerTransform later when Player is ready.
    }

    /// <summary>
    /// Sets the player transform for the follow camera. Call this when player is spawned/ready.
    /// </summary>
    public void SetPlayerTransform(Transform player) {
         playerTransform = player;
         AssignPlayerToFollowCamera();
    }

    private void AssignPlayerToFollowCamera()
    {
         if (playerFollowCamera != null && playerTransform != null)
         {
              playerFollowCamera.Follow = playerTransform;
              playerFollowCamera.LookAt = playerTransform;
              Debug.Log($"[CameraManager] Assigned Player to '{playerFollowCamera.name}'.");
              // Ensure player camera is active initially
              ReturnToPlayerFollow(0f); // Instant switch
         }
    }

    // --- Public Methods for Cutscene Control ---

    /// <summary>
    /// Switches the active camera focus to a specific target transform.
    /// This example assumes you have a dedicated VCam for this or repurposes the player cam.
    /// </summary>
    /// <param name="target">The Transform to focus on.</param>
    /// <param name="blendTimeOverride">Optional override for blend time (-1 uses default).</param>
    public void FocusOnTarget(Transform target, float blendTimeOverride = -1f)
    {
        if (target == null) { Debug.LogWarning("FocusOnTarget called with null target."); return; }

        Debug.Log($"[CameraManager] Focusing on target '{target.name}'.");

        // Option 1: Re-target player camera (simple, less flexible)
        // if (playerFollowCamera != null)
        // {
        //     playerFollowCamera.Follow = target;
        //     playerFollowCamera.LookAt = target;
        //     SwitchToVirtualCamera(playerFollowCamera, blendTimeOverride); // Ensure it has priority
        // }

        // Option 2: Switch to a dedicated virtual camera setup for focusing targets
        // This requires you to create such a camera in your scene.
        // Example: Find a generic focus cam and set its target
         CinemachineVirtualCamera focusCam = FindVirtualCameraByName("VCam_FocusTarget"); // Example name
         if (focusCam != null)
         {
              focusCam.Follow = target;
              focusCam.LookAt = target;
              SwitchToVirtualCamera(focusCam, blendTimeOverride);
         }
         else { Debug.LogError("FocusOnTarget: Could not find 'VCam_FocusTarget'. Create this VCam or adjust logic."); }
    }

    /// <summary>
    /// Switches back to the default camera following the player.
    /// </summary>
    /// <param name="blendTimeOverride">Optional override for blend time (-1 uses default).</param>
    public void ReturnToPlayerFollow(float blendTimeOverride = -1f)
    {
        Debug.Log("CameraManager: Returning focus to Player.");
        if (playerFollowCamera != null)
        {
            // Ensure player transform is still valid if player can be destroyed/respawned
            if (playerTransform == null && Player.Instance != null) playerTransform = Player.Instance.transform;

            if(playerTransform != null) {
                 playerFollowCamera.Follow = playerTransform;
                 playerFollowCamera.LookAt = playerTransform;
            } else {
                 Debug.LogWarning("ReturnToPlayerFollow: Player transform is null.");
                 // Maybe focus on a default point?
            }
            SwitchToVirtualCamera(playerFollowCamera, blendTimeOverride); // Give priority back
        } else { Debug.LogError("ReturnToPlayerFollow: playerFollowCamera reference is null!"); }
    }

    /// <summary>
    /// Switches to a specific Cinemachine Virtual Camera by increasing its priority.
    /// Assumes other cameras have lower priority (default 10).
    /// </summary>
    /// <param name="virtualCamera">The virtual camera component to activate.</param>
    /// <param name="blendTimeOverride">Optional override for blend time (-1 uses default).</param>
    public void SwitchToVirtualCamera(CinemachineVirtualCamera virtualCamera, float blendTimeOverride = -1f)
    {
        if (virtualCamera == null) { Debug.LogError("SwitchToVirtualCamera called with null camera!"); return; }

        // Set blend time if override is provided
        if (blendTimeOverride >= 0f && cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Time = blendTimeOverride;
        }

        Debug.Log($"[CameraManager] Switching to Virtual Camera '{virtualCamera.name}'. Setting priority high.");
        // Lower priority of all other VCams first? More robust but requires tracking them.
        // Simple approach: Set target high, assume player cam is default low.
        // Make sure playerFollowCamera has default priority (e.g., 10)
        if (playerFollowCamera != null && playerFollowCamera != virtualCamera) playerFollowCamera.Priority = 10;
        // Set target camera's priority higher to make it active
        virtualCamera.Priority = 11;

        // Note: Resetting blend time back to default might be needed after the blend finishes.
        // This can be done via a coroutine or by setting it again when switching back.
    }

    // Helper to find a VCam (you might want a more robust dictionary-based approach)
    private CinemachineVirtualCamera FindVirtualCameraByName(string name)
    {
         // Inefficient - use tags or a dictionary populated at start if performance matters
         var vcams = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
         foreach(var vcam in vcams)
         {
              if(vcam.gameObject.name == name) return vcam;
         }
         return null;
    }
}
