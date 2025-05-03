using UnityEngine;
using UnityEngine.SceneManagement; // Needed for Scene operations / 场景操作所需
using System.Collections.Generic;
using System.Linq; // Needed for LINQ operations like Any(), FindObjectsByType / LINQ 操作（如 Any(), FindObjectsByType）所需

/// <summary>
/// Manages the state of interactable items AND key NPCs within this specific scene
/// for the current game session (in-memory). Handles registration, saving the current
/// state before unload, and loading state after load.
/// 管理此特定场景中可交互物品和关键 NPC 的当前游戏会话状态（内存中）。
/// 处理注册、卸载前保存当前状态以及加载后加载状态。
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Tooltip("The unique name of this scene. MUST match the scene file name exactly for SessionStateManager.\n此场景的唯一名称。必须与场景文件名完全匹配，以便 SessionStateManager 使用。")]
    [SerializeField] public string sceneName;

    // --- Registries / 注册表 ---
    // Stores references to Item components in this scene / 存储此场景中 Item 组件的引用
    private Dictionary<string, Item> registeredItems = new Dictionary<string, Item>();
    // Stores references to SavableNPC components in this scene / 存储此场景中 SavableNPC 组件的引用
    private Dictionary<string, SavableNPC> registeredNPCs = new Dictionary<string, SavableNPC>();

    // --- State Tracking / 状态跟踪 ---
    // Tracks uniqueIDs of items picked up this session / 跟踪此会话中拾取的物品的 uniqueID
    private HashSet<string> pickedUpItemIDs = new HashSet<string>();
    // Tracks the last known active state for NPCs, used for saving / 跟踪 NPC 的最后已知活动状态，用于保存
    private Dictionary<string, bool> npcActiveStates = new Dictionary<string, bool>();

    #region Unity Lifecycle / Unity 生命周期
    void Awake()
    {
        // Validate scene name assignment / 验证场景名称分配
        if (string.IsNullOrEmpty(sceneName))
        {
            // Attempt to use the actual scene name as a fallback / 尝试使用实际场景名称作为后备
            sceneName = gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[GameSceneManager] GameSceneManager on GameObject '{gameObject.name}' is missing its Scene Name and couldn't get it from the scene!", this.gameObject);
            } else {
                 Debug.LogWarning($"[GameSceneManager] GameSceneManager on GameObject '{gameObject.name}' was missing Scene Name. Automatically assigned to '{sceneName}'.", this.gameObject);
            }
        }

        // Clear registries on awake (in case of editor reloads or complex scenarios)
        // 在 Awake 时清除注册表（以防编辑器重新加载或复杂情况）
        registeredItems.Clear();
        registeredNPCs.Clear();
        pickedUpItemIDs.Clear();
        npcActiveStates.Clear();
        // Debug.Log($"[GameSceneManager:{sceneName}] Awake completed. Registries cleared.");
    }
    #endregion

    #region Registration Methods / 注册方法
    // --- Item Registration / 物品注册 ---
    /// <summary>
    /// Called by Item scripts in their Start() method to register themselves.
    /// 由 Item 脚本在其 Start() 方法中调用以注册自身。
    /// </summary>
    public void RegisterItem(Item item)
    {
        if (item == null || string.IsNullOrEmpty(item.uniqueID))
        {
            Debug.LogError("[GameSceneManager] Attempted to register an invalid item (null or missing uniqueID).", item?.gameObject);
            return;
        }

        if (!registeredItems.ContainsKey(item.uniqueID))
        {
            registeredItems.Add(item.uniqueID, item);
            // Debug.Log($"[GameSceneManager:{sceneName}] Registered item: {item.uniqueID}");
        }
        else
        {
            Debug.LogWarning($"[GameSceneManager:{sceneName}] Duplicate item uniqueID registration attempt: {item.uniqueID}. GameObject: {item.gameObject.name}", item.gameObject);
        }
    }

    /// <summary>
    /// Called by ItemPickup scripts *before* they destroy themselves.
    /// 由 ItemPickup 脚本在销毁自身*之前*调用。
    /// </summary>
    public void NotifyItemPickedUp(string uniqueID)
    {
        if (string.IsNullOrEmpty(uniqueID)) return;

        pickedUpItemIDs.Add(uniqueID); // Mark as picked up for saving / 标记为已拾取以便保存
        if (registeredItems.ContainsKey(uniqueID)) // Remove from active registry / 从活动注册表中移除
        {
            registeredItems.Remove(uniqueID);
        }
        // Debug.Log($"[GameSceneManager:{sceneName}] Item '{uniqueID}' marked as picked up.");
    }

    // --- NPC Registration / NPC 注册 ---
    /// <summary>
    /// Called by SavableNPC scripts in their Start() method.
    /// 由 SavableNPC 脚本在其 Start() 方法中调用。
    /// </summary>
    public void RegisterSavableNPC(SavableNPC npc)
    {
        if (npc == null || string.IsNullOrEmpty(npc.uniqueID))
        {
            Debug.LogError("[GameSceneManager] Attempted to register an invalid NPC (null or missing uniqueID).", npc?.gameObject);
            return;
        }
        if (!registeredNPCs.ContainsKey(npc.uniqueID))
        {
            registeredNPCs.Add(npc.uniqueID, npc);
            // Record initial active state ONLY if not already tracked from loading
            // 仅当尚未从加载中跟踪时才记录初始活动状态
            if (!npcActiveStates.ContainsKey(npc.uniqueID))
            {
                npcActiveStates[npc.uniqueID] = npc.gameObject.activeSelf;
            }
            // Debug.Log($"[GameSceneManager:{sceneName}] Registered NPC: '{npc.uniqueID}'. Current Active: {npc.gameObject.activeSelf}. Tracked State for Save: {npcActiveStates[npc.uniqueID]}");
        }
        else
        {
            Debug.LogWarning($"[GameSceneManager:{sceneName}] Duplicate NPC uniqueID registration attempt: {npc.uniqueID}. GameObject: {npc.gameObject.name}", npc.gameObject);
        }
    }

     /// <summary>
    /// Called by SavableNPC right before its state changes (usually deactivation via Timeline or code).
    /// Updates the state tracked for saving.
    /// 由 SavableNPC 在其状态更改（通常是通过 Timeline 或代码停用）之前立即调用。
    /// 更新用于保存的跟踪状态。
    /// </summary>
    public void NotifyNPCSavedStateChange(string uniqueID, bool isActive)
    {
         if (string.IsNullOrEmpty(uniqueID)) return;
         // This tracks the state intended for the *next* save
         // 这会跟踪用于*下次*保存的状态
         npcActiveStates[uniqueID] = isActive;
         // Debug.Log($"[GameSceneManager:{sceneName}] Notified NPC '{uniqueID}' state change for saving. IsActive: {isActive}");
    }
    #endregion

    #region Save/Load Logic / 保存/加载逻辑
    /// <summary>
    /// Collects the current state of all relevant items and NPCs in this scene.
    /// Called by TransitionManager before the scene is unloaded.
    /// 收集此场景中所有相关物品和 NPC 的当前状态。
    /// 由 TransitionManager 在场景卸载之前调用。
    /// </summary>
    /// <returns>A SceneSaveData object containing the state for this scene. / 包含此场景状态的 SceneSaveData 对象。</returns>
    public SceneSaveData SaveCurrentState()
    {
        // Debug.Log($"[GameSceneManager:{sceneName}] Saving state...");
        SceneSaveData sceneSaveData = new SceneSaveData(); // Initializes internal lists / 初始化内部列表

        // --- Save Item State / 保存物品状态 ---
        // Save state of currently registered (existing) items
        // 保存当前已注册（存在）物品的状态
        foreach (var pair in registeredItems)
        {
            string uniqueID = pair.Key;
            Item item = pair.Value;
            if (item == null) continue;

            ItemSaveData itemSaveData = new ItemSaveData { isPresent = true };

            if (item is ItemCheckUp checkUp)
            {
                itemSaveData.type = "ItemCheckUp";
                itemSaveData.hasBeenChecked = checkUp.hasBeenChecked;
            }
            else { itemSaveData.type = item.GetType().Name; }
            sceneSaveData.itemsState.Add(new ItemStatePair { uniqueID = uniqueID, itemState = itemSaveData });
        }
        // Add entries for items that were picked up
        // 为已拾取的物品添加条目
        foreach (string pickedUpID in pickedUpItemIDs)
        {
             if (!sceneSaveData.itemsState.Any(statePair => statePair.uniqueID == pickedUpID))
             {
                  ItemSaveData itemSaveData = new ItemSaveData { type = "ItemPickup", isPresent = false, hasBeenChecked = false };
                  sceneSaveData.itemsState.Add(new ItemStatePair { uniqueID = pickedUpID, itemState = itemSaveData });
             }
        }
        // --- End Save Item State / 结束保存物品状态 ---


        // --- Save NPC State / 保存 NPC 状态 ---
        // Save state based on the last known state stored in npcActiveStates
        // 根据存储在 npcActiveStates 中的最后已知状态保存状态
        foreach(var pair in npcActiveStates)
        {
             string uniqueID = pair.Key;
             bool isActive = pair.Value;
             // Ensure no duplicates / 确保没有重复项
             if (!sceneSaveData.npcsState.Any(statePair => statePair.uniqueID == uniqueID))
             {
                   NPCSaveData npcSaveData = new NPCSaveData { isActive = isActive };
                   sceneSaveData.npcsState.Add(new NPCStatePair { uniqueID = uniqueID, npcState = npcSaveData });
                    // Debug.Log($"[GameSceneManager:{sceneName}] Saving NPC '{uniqueID}' state (from tracked state). IsActive: {isActive}");
             }
        }
        // Also save the *current* state of any registered NPCs whose state wasn't explicitly tracked yet
        // 同时保存任何已注册但其状态尚未明确跟踪的 NPC 的*当前*状态
        foreach(var pair in registeredNPCs)
        {
            string uniqueID = pair.Key;
            // Check if it exists, wasn't already saved via npcActiveStates, and the instance is valid
            // 检查它是否存在，尚未通过 npcActiveStates 保存，并且实例有效
            if (pair.Value != null && !npcActiveStates.ContainsKey(uniqueID) && !sceneSaveData.npcsState.Any(sp => sp.uniqueID == uniqueID))
            {
                 bool currentActualState = pair.Value.gameObject.activeSelf;
                 NPCSaveData npcSaveData = new NPCSaveData { isActive = currentActualState };
                 sceneSaveData.npcsState.Add(new NPCStatePair { uniqueID = uniqueID, npcState = npcSaveData });
                 // Debug.Log($"[GameSceneManager:{sceneName}] Saving registered (but not notified) NPC '{uniqueID}' current state. IsActive: {currentActualState}");
            }
        }
        // --- End Save NPC State / 结束保存 NPC 状态 ---

        // Debug.Log($"[GameSceneManager:{sceneName}] Saved state for {sceneSaveData.itemsState.Count} items and {sceneSaveData.npcsState.Count} NPCs.");
        return sceneSaveData;
    }

    /// <summary>
    /// Applies a previously saved state to the items and NPCs in this scene.
    /// Called by TransitionManager after this scene has loaded and become active (with a frame delay).
    /// 将先前保存的状态应用于此场景中的物品和 NPC。
    /// 由 TransitionManager 在此场景加载并变为活动状态（有一帧延迟）后调用。
    /// </summary>
    /// <param name="sceneSaveData">The state data retrieved from SessionStateManager. / 从 SessionStateManager 检索到的状态数据。</param>
    public void LoadSaveData(SceneSaveData sceneSaveData)
    {
        if (sceneSaveData == null)
        {
            Debug.LogWarning($"[GameSceneManager:{sceneName}] LoadSaveData called with null data. Scene will use default state.");
            return;
        }
        Debug.Log($"[GameSceneManager:{sceneName}] === Starting LoadSaveData ===");

        // --- Load Item State / 加载物品状态 ---
        List<Item> itemsToDestroy = new List<Item>();
        if (sceneSaveData.itemsState != null)
        {
            // Iterate through saved item states / 遍历保存的物品状态
            foreach (ItemStatePair pair in sceneSaveData.itemsState)
            {
                if (string.IsNullOrEmpty(pair.uniqueID) || pair.itemState == null) { Debug.LogWarning($"[GameSceneManager:{sceneName}] Skipping invalid ItemStatePair."); continue; }

                // Try find the item currently registered / 尝试查找当前注册的物品
                if (registeredItems.TryGetValue(pair.uniqueID, out Item item))
                {
                    if (item != null) // Check if instance exists / 检查实例是否存在
                    {
                        if (!pair.itemState.isPresent) // Should not be present / 不应存在
                        {
                            itemsToDestroy.Add(item);
                            pickedUpItemIDs.Add(item.uniqueID);
                        }
                        else // Should be present, apply state / 应存在，应用状态
                        {
                            if (item is ItemCheckUp checkUp && pair.itemState.type == "ItemCheckUp")
                            {
                                checkUp.SetCheckedState(pair.itemState.hasBeenChecked);
                            }
                            // Add other item type state loading here / 在此处添加其他物品类型状态加载
                            pickedUpItemIDs.Remove(item.uniqueID);
                        }
                    } else { registeredItems.Remove(pair.uniqueID); } // Clean registry if instance is null / 如果实例为 null 则清理注册表
                }
                else // Not registered / 未注册
                {
                    if (!pair.itemState.isPresent) { pickedUpItemIDs.Add(pair.uniqueID); } // Track as picked up / 跟踪为已拾取
                    else { Debug.LogWarning($"[GameSceneManager:{sceneName}] Item '{pair.uniqueID}' expected present but not registered."); }
                }
            }
            // Destroy marked items / 销毁标记的物品
            foreach (Item itemToDestroy in itemsToDestroy)
            {
                if (itemToDestroy != null)
                {
                    registeredItems.Remove(itemToDestroy.uniqueID);
                    Destroy(itemToDestroy.gameObject);
                }
            }
        } else { Debug.LogWarning($"[GameSceneManager:{sceneName}] itemsState list is null in saved data."); }
        // --- End Load Item State / 结束加载物品状态 ---


        // --- Load NPC State / 加载 NPC 状态 ---
        if (sceneSaveData.npcsState != null)
        {
            // Debug.Log($"[GameSceneManager:{sceneName}] Processing {sceneSaveData.npcsState.Count} NPC states from save data...");
            // Proactively find all SavableNPCs in the scene right now / 主动查找当前场景中的所有 SavableNPC
            Dictionary<string, SavableNPC> npcsInSceneNow = FindObjectsByType<SavableNPC>(FindObjectsSortMode.None)
                                                            .Where(npc => npc != null && !string.IsNullOrEmpty(npc.uniqueID)) // Ensure valid NPCs / 确保 NPC 有效
                                                            .GroupBy(npc => npc.uniqueID) // Handle potential duplicates gracefully / 优雅地处理潜在的重复项
                                                            .ToDictionary(g => g.Key, g => g.First());
            // Debug.Log($"[GameSceneManager:{sceneName}] Proactively found {npcsInSceneNow.Count} SavableNPCs with unique IDs currently in scene.");

            // Iterate through saved NPC states / 遍历保存的 NPC 状态
            foreach (NPCStatePair pair in sceneSaveData.npcsState)
            {
                if (string.IsNullOrEmpty(pair.uniqueID) || pair.npcState == null) { Debug.LogWarning($"[GameSceneManager:{sceneName}] Skipping invalid NPCStatePair."); continue; }

                bool savedIsActive = pair.npcState.isActive;
                // Update the state tracker immediately based on save data / 立即根据保存数据更新状态跟踪器
                npcActiveStates[pair.uniqueID] = savedIsActive;
                // Debug.Log($"[GameSceneManager:{sceneName}] Processing saved NPC '{pair.uniqueID}'. Saved State: IsActive={savedIsActive}.");

                // Try find the NPC instance *currently in the scene* / 尝试查找*当前场景中*的 NPC 实例
                if (npcsInSceneNow.TryGetValue(pair.uniqueID, out SavableNPC npcInstance))
                {
                    if (npcInstance != null)
                    {
                        // Debug.Log($"[GameSceneManager:{sceneName}] --> Found NPC '{pair.uniqueID}' instance in scene. Current GO Active: {npcInstance.gameObject.activeSelf}");
                        // Apply the saved active state IF different / 如果不同则应用保存的活动状态
                        if (npcInstance.gameObject.activeSelf != savedIsActive)
                        {
                            // Debug.Log($"[GameSceneManager:{sceneName}] ----> Applying state: Setting NPC '{pair.uniqueID}' GameObject Active = {savedIsActive}");
                            npcInstance.gameObject.SetActive(savedIsActive);
                        }
                        // Ensure it's registered if LoadSaveData runs before its Start method
                        // 确保现在注册它（如果 LoadSaveData 在其 Start 方法之前运行）
                        if (!registeredNPCs.ContainsKey(pair.uniqueID))
                        {
                             registeredNPCs.Add(pair.uniqueID, npcInstance);
                             // Debug.Log($"[GameSceneManager:{sceneName}] ----> Registered NPC '{pair.uniqueID}' during load application.");
                        }
                    }
                    else { Debug.LogWarning($"[GameSceneManager:{sceneName}] NPC '{pair.uniqueID}' found in scene search but instance is null?"); }
                }
                else // NPC from save data not found in the current scene objects / 在当前场景对象中找不到来自保存数据的 NPC
                {
                    if (!savedIsActive)
                    {
                         // Expected for an NPC saved as inactive. State is tracked.
                         // 对于保存为非活动的 NPC，这是预期的。状态已被跟踪。
                         // Debug.Log($"[GameSceneManager:{sceneName}] --> NPC '{pair.uniqueID}' not found in scene, correct as Saved State is IsActive=false.");
                    }
                    else
                    {
                         // PROBLEM: Should be active but isn't in the scene.
                         // 问题：应该处于活动状态但不在场景中。
                         Debug.LogWarning($"[GameSceneManager:{sceneName}] --> PROBLEM: NPC '{pair.uniqueID}' not found in scene, but Saved State is IsActive=true! Check prefab/ID.");
                         // State already tracked as inactive from save data / 状态已根据保存数据跟踪为非活动
                    }
                }
            }
        } else { Debug.LogWarning($"[GameSceneManager:{sceneName}] npcsState list is null in saved data."); }
        // --- End Load NPC State / 结束加载 NPC 状态 ---

        Debug.Log($"[GameSceneManager:{sceneName}] === Finished LoadSaveData ===");
    }
    #endregion

    #region Post-Cutscene State (Optional) / 过场动画后状态（可选）
    // This method remains for applying specific, non-standard state changes after cutscenes
    // if needed, beyond simple activation/deactivation handled by saving/loading.
    // 如果需要，此方法保留用于在过场动画后应用特定的、非标准的状态更改，
    // 超出通过保存/加载处理的简单激活/停用。
    public void ApplyPostCutsceneState(string stateKey)
    {
        // Debug.Log($"[GameSceneManager:{sceneName}] Applying post-cutscene state for key: '{stateKey}'");
        // Implement specific logic based on stateKey if required / 如果需要，根据 stateKey 实现特定逻辑
    }
    #endregion

    #region Helper Methods (Internal) / 辅助方法（内部）
    // Helper to find item internally (if needed beyond registration)
    // 内部查找物品的辅助方法（如果注册之外需要）
    private Item FindItemByID_Internal(string id) {
        registeredItems.TryGetValue(id, out Item item);
        return item;
    }
    #endregion
}
