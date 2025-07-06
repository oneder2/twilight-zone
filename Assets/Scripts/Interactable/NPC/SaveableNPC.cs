using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Base class for NPCs whose active state needs to be saved and restored across scene loads within a session.
/// Handles registration with the GameSceneManager.
/// 需要在会话内的场景加载中保存和恢复其活动状态的 NPC 的基类。
/// 处理向 GameSceneManager 的注册。
/// </summary>
public abstract class SavableNPC : Interactable // Inherit from Interactable / 继承自 Interactable
{
    [Header("Saving State / 保存状态")]
    [Tooltip("A unique identifier for this specific NPC instance within the scene. MUST BE UNIQUE per scene.\n此场景中此特定 NPC 实例的唯一标识符。每个场景中必须唯一。")]
    public string uniqueID; // Assign a unique ID in the Inspector / 在 Inspector 中分配唯一 ID

    protected GameSceneManager ownerSceneManager; // Reference to the scene manager / 对场景管理器的引用

    protected override void Start()
    {
        base.Start(); // Call Interactable.Start() for marker setup / 调用 Interactable.Start() 进行标记设置

        // --- Robust NPC Registration / 健壮的 NPC 注册 ---
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError($"[SavableNPC] SavableNPC '{gameObject.name}' is missing its uniqueID!", gameObject);
            return; // Cannot register without ID / 没有 ID 无法注册
        }

        ownerSceneManager = FindSceneManager();
        if (ownerSceneManager != null)
        {
            ownerSceneManager.RegisterSavableNPC(this);
            // Debug.Log($"[SavableNPC] NPC '{uniqueID}' registered with GameSceneManager in scene '{gameObject.scene.name}'.");
        }
        else
        {
            Debug.LogError($"[SavableNPC] SavableNPC '{uniqueID}' could not find GameSceneManager within its own scene ('{gameObject.scene.name}')!", gameObject);
        }
        // --- End Robust NPC Registration / 结束健壮的 NPC 注册 ---
    }

    /// <summary>
    /// Finds the GameSceneManager in the current scene.
    /// 查找当前场景中的 GameSceneManager。
    /// </summary>
    protected GameSceneManager FindSceneManager()
    {
        Scene currentScene = gameObject.scene;
        if (!currentScene.IsValid() || !currentScene.isLoaded) return null;

        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            // Search including inactive GameObjects / 搜索包括非活动 GameObject
            GameSceneManager managerInScene = root.GetComponentInChildren<GameSceneManager>(true);
            if (managerInScene != null) return managerInScene;
        }
        return null;
    }

    /// <summary>
    /// Call this method right before the NPC's GameObject state changes for saving purposes.
    /// 在 NPC 的 GameObject 状态更改以进行保存之前立即调用此方法。
    /// </summary>
    /// <param name="isActive">The state the NPC will have when saved. / NPC 保存时将具有的状态。</param>
    public void NotifySavedStateChange(bool isActive)
    {
        // Try finding manager again if it was null before / 如果之前为 null，则再次尝试查找管理器
        if (ownerSceneManager == null && !string.IsNullOrEmpty(uniqueID)) {
             ownerSceneManager = FindSceneManager();
        }
        // Notify the manager of the state change / 通知管理器状态更改
        ownerSceneManager?.NotifyNPCSavedStateChange(this.uniqueID, isActive);
    }

    /// <summary>
    /// Deactivates the NPC GameObject and notifies the GameSceneManager.
    /// Use this instead of directly calling SetActive(false) when an NPC is "killed" or removed.
    /// 停用 NPC GameObject 并通知 GameSceneManager。
    /// 当 NPC 被“杀死”或移除时，使用此方法代替直接调用 SetActive(false)。
    /// </summary>
    protected virtual void DeactivateAndSaveState()
    {
        // Debug.Log($"[SavableNPC:{uniqueID}] Attempting DeactivateAndSaveState...");
        NotifySavedStateChange(false); // Notify manager *before* deactivating / 在停用*之前*通知管理器
        // Debug.Log($"[SavableNPC:{uniqueID}] Setting GameObject active = false.");
        gameObject.SetActive(false);   // Deactivate using code / 使用代码停用
    }

    // Optional: Unregister on destroy? Less critical as scene unload handles it.
    // 可选：在销毁时注销？不太重要，因为场景卸载会处理它。
    // protected virtual void OnDestroy() { base.OnDestroy(); /* Unregister logic if needed */ }
}
