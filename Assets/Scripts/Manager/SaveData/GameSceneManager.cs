using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理单个场景的保存和加载逻辑，每个场景应有一个独立的 GameSceneManager 实例。
/// 负责记录场景中物品的状态，并在场景切换时保存和恢复这些状态。
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    /// <summary>
    /// 当前场景的名称，用于标识保存数据对应的场景。
    /// 在 Inspector 中配置，必须与 Unity 的 SceneManager.GetActiveScene().name 一致。
    /// </summary>
    [SerializeField] 
    public string sceneName;

    /// <summary>
    /// 场景中所有可交互物品的唯一标识符列表。
    /// 在 Inspector 中配置，列出场景中需要保存状态的物品的 uniqueID。
    /// 初始化为空列表，避免运行时出现 NullReferenceException。
    /// </summary>
    [SerializeField] 
    private List<string> itemUniqueIDs = new List<string>();

    /// <summary>
    /// 用于在 Inspector 中配置物品类型（uniqueID 与类型的映射）。
    /// 因为 Unity 不直接支持 Dictionary 的序列化，使用此列表作为中间数据结构。
    /// </summary>
    [SerializeField] 
    private List<ItemTypePair> itemTypePairs = new List<ItemTypePair>();

    /// <summary>
    /// 运行时使用的物品类型字典，存储 uniqueID 与类型（"ItemPickup" 或 "ItemCheckUp"）的映射。
    /// 从 itemTypePairs 初始化，避免在 Inspector 中手动编辑 Dictionary。
    /// </summary>
    public Dictionary<string, string> itemTypes = new Dictionary<string, string>();

    /// <summary>
    /// 定义 uniqueID 和 itemType 的键值对结构，用于序列化。
    /// 在 Inspector 中以列表形式显示，便于配置。
    /// </summary>
    [System.Serializable]
    public class ItemTypePair
    {
        public string uniqueID; // 物品的唯一标识符，与 itemUniqueIDs 对应
        public string itemType; // 物品类型，例如 "ItemPickup" 或 "ItemCheckUp"
    }

    /// <summary>
    /// 在对象唤醒时初始化字段，确保所有数据结构非空，并从 itemTypePairs 填充 itemTypes。
    /// </summary>
    private void Awake()
    {
        // 检查并初始化 itemUniqueIDs，避免空引用
        if (itemUniqueIDs == null)
        {
            itemUniqueIDs = new List<string>();
            Debug.LogWarning("itemUniqueIDs was null, initialized as empty list.");
        }

        // 检查并初始化 itemTypes，避免空引用
        if (itemTypes == null)
        {
            itemTypes = new Dictionary<string, string>();
            Debug.LogWarning("itemTypes was null, initialized as empty dictionary.");
        }

        // 检查并初始化 itemTypePairs，避免空引用
        if (itemTypePairs == null)
        {
            itemTypePairs = new List<ItemTypePair>();
        }

        // 示例：手动填充 itemTypes
        if (itemUniqueIDs.Count > 0 && itemTypes.Count == 0)
        {
            foreach (string id in itemUniqueIDs)
            {
                if (!itemTypes.ContainsKey(id))
                {
                    // 根据你的场景需求手动指定类型
                    if (id.StartsWith("item_pick")) // 示例规则
                        itemTypes[id] = "ItemPickup";
                    else if (id.StartsWith("item_check"))
                        itemTypes[id] = "ItemCheckUp";
                    else
                        itemTypes[id] = "Unknown"; // 默认值
                }
            }
        }

        // 将 Inspector 中的 itemTypePairs 转换为运行时的 itemTypes 字典
        foreach (var pair in itemTypePairs)
        {
            // 确保 uniqueID 非空且未重复添加
            if (!string.IsNullOrEmpty(pair.uniqueID) && !itemTypes.ContainsKey(pair.uniqueID))
            {
                itemTypes[pair.uniqueID] = pair.itemType;
            }
        }
    }

    /// <summary>
    /// 保存当前场景的状态，返回一个 SceneSaveData 对象。
    /// 遍历 itemUniqueIDs，记录每个物品的当前状态（如是否存在、是否被检查）。
    /// </summary>
    /// <returns>包含场景物品状态的 SceneSaveData 对象</returns>
    public SceneSaveData SaveCurrentState()
    {
        // 创建新的保存数据对象，初始化 itemsState 列表
        SceneSaveData sceneSaveData = new SceneSaveData { itemsState = new List<ItemStatePair>() };

        // 遍历所有需要保存的物品
        foreach (string uniqueID in itemUniqueIDs)
        {
            // 通过 ItemHelper 获取场景中的物品实例
            Item item = ItemHelper.GetItemByUniqueID(uniqueID);

            // 创建物品保存数据，设置类型（从 itemTypes 获取，若无则为 "Unknown"）
            ItemSaveData itemSaveData = new ItemSaveData
            {
                type = itemTypes.ContainsKey(uniqueID) ? itemTypes[uniqueID] : "Unknown"
            };

            // 根据物品类型处理保存逻辑
            if (itemSaveData.type == "ItemPickup")
            {
                // 可拾取物品：检查物品是否仍存在（被拾取后销毁则为 null）
                itemSaveData.isPresent = (item != null);
                itemSaveData.hasBeenChecked = false; // 可拾取物品无需检查状态，固定为 false
            }
            else if (itemSaveData.type == "ItemCheckUp")
            {
                // 可检查物品：始终存在（不会被销毁）
                itemSaveData.isPresent = true;
                // 转换为 ItemCheckUp 类型，检查其 hasBeenChecked 属性
                ItemCheckUp checkUp = item as ItemCheckUp;
                itemSaveData.hasBeenChecked = (checkUp != null && checkUp.hasBeenChecked);
            }
            else
            {
                // 未知类型：记录警告并跳过此物品
                Debug.LogWarning($"Unknown item type for uniqueID: {uniqueID}");
                continue;
            }

            // 将物品状态添加到保存数据中
            sceneSaveData.itemsState.Add(new ItemStatePair
            {
                uniqueID = uniqueID,
                itemState = itemSaveData
            });
        }

        return sceneSaveData;
    }

    /// <summary>
    /// 根据保存数据加载并应用场景状态。
    /// 遍历保存数据中的物品状态，更新场景中对应物品的实际状态。
    /// </summary>
    /// <param name="sceneSaveData">从 SaveSystem 提供的场景保存数据</param>
    public void LoadSaveData(SceneSaveData sceneSaveData)
    {
        // 检查保存数据是否有效
        if (sceneSaveData == null || sceneSaveData.itemsState == null)
        {
            Debug.LogWarning("No save data to load for scene: " + sceneName);
            return;
        }

        // 遍历保存数据中的每个物品状态
        foreach (ItemStatePair pair in sceneSaveData.itemsState)
        {
            // 获取场景中的物品实例
            Item item = ItemHelper.GetItemByUniqueID(pair.uniqueID);
            if (item != null)
            {
                if (pair.itemState.type == "ItemPickup" && !pair.itemState.isPresent)
                {
                    // 可拾取物品：如果保存数据表示已拾取（isPresent = false），销毁物体
                    Destroy(item.gameObject);
                }
                else if (pair.itemState.type == "ItemCheckUp")
                {
                    // 可检查物品：恢复检查状态
                    ItemCheckUp checkUp = item as ItemCheckUp;
                    if (checkUp != null)
                    {
                        checkUp.hasBeenChecked = pair.itemState.hasBeenChecked;
                    }
                }
            }
        }
    }
}