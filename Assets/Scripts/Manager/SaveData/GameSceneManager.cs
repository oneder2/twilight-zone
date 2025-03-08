using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : Singleton<GameSceneManager>
{
    [SerializeField] public string sceneName;
    [SerializeField] private List<string> itemUniqueIDs = new List<string>(); // 初始化为空列表
    public Dictionary<string, string> itemTypes = new Dictionary<string, string>(); // 初始化为空字典

    protected override void Awake()
    {
        base.Awake(); // 调用基类的单例初始化
        // 确保字段已初始化（冗余检查）
        if (itemUniqueIDs == null)
        {
            itemUniqueIDs = new List<string>();
            Debug.LogWarning("itemUniqueIDs was null, initialized as empty list.");
        }

        if (itemTypes == null)
        {
            itemTypes = new Dictionary<string, string>();
            Debug.LogWarning("itemTypes was null, initialized as empty dictionary.");
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
    }

    // 保存当前场景状态
    public SceneSaveData SaveCurrentState()
    {
        SceneSaveData sceneSaveData = new SceneSaveData { itemsState = new List<ItemStatePair>() };

        // 逐个遍历场景中的物体
        foreach (string uniqueID in itemUniqueIDs)
        {
            Item item = ItemHelper.GetItemByUniqueID(uniqueID);
            ItemSaveData itemSaveData = new ItemSaveData
            {
                type = itemTypes.ContainsKey(uniqueID) ? itemTypes[uniqueID] : "Unknown"
            };

            // 可拾取物体
            // 1. 物品是否有效（被捡起）
            // 2. 物品是否被检查（由于可拾取物体只能被检查一次，所以默认为false）
            if (itemSaveData.type == "ItemPickup")
            {
                itemSaveData.isPresent = (item != null);
                itemSaveData.hasBeenChecked = false;
            }

            // 可检查物体
            // 1. 物体是否有效（由于可检查物体一般有效，所以默认为true）_____注意这里未来可能会存在拓展逻辑_____
            // 2. 物体是否被捡起
            // 3. 物体是否被检查
            else if (itemSaveData.type == "ItemCheckUp")
            {
                itemSaveData.isPresent = true;
                ItemCheckUp checkUp = item as ItemCheckUp;
                itemSaveData.hasBeenChecked = (checkUp != null && checkUp.hasBeenChecked);
            }

            // 意料之外的物体（）_____注意这里未来可能会存在拓展逻辑_____
            else
            {
                Debug.LogWarning($"Unknown item type for uniqueID: {uniqueID}");
                continue;
            }

            // 存储保存信息到本场景存档
            sceneSaveData.itemsState.Add(new ItemStatePair
            {
                uniqueID = uniqueID,
                itemState = itemSaveData
            });
        }

        return sceneSaveData;
    }

    // 加载并应用保存数据
    public void LoadSaveData(SceneSaveData sceneSaveData)
    {
        if (sceneSaveData == null || sceneSaveData.itemsState == null)
        {
            Debug.LogWarning("No save data to load for scene: " + sceneName);
            return;
        }

        foreach (ItemStatePair pair in sceneSaveData.itemsState)
        {
            Item item = ItemHelper.GetItemByUniqueID(pair.uniqueID);
            if (item != null)
            {
                if (pair.itemState.type == "ItemPickup" && !pair.itemState.isPresent)
                {
                    Destroy(item.gameObject);
                }
                else if (pair.itemState.type == "ItemCheckUp")
                {
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