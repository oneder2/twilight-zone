using System;
using System.Collections.Generic; // Required for List<>

// --- Item Saving Structures ---
// --- 物品保存结构体 ---

[Serializable]
public class ItemSaveData
{
    public string type; // e.g., "ItemPickup", "ItemCheckUp" / 例如 "ItemPickup", "ItemCheckUp"
    public bool isPresent; // Used by ItemPickup / 由 ItemPickup 使用
    public bool hasBeenChecked; // Used by ItemCheckUp / 由 ItemCheckUp 使用
}

[Serializable]
public class ItemStatePair
{
    public string uniqueID;
    public ItemSaveData itemState;
}

// --- NPC Saving Structures ---
// --- NPC 保存结构体 ---

[Serializable]
public class NPCSaveData
{
    public bool isActive; // Is the NPC currently active in the scene? / NPC 当前在场景中是否活动？
    // Add other NPC-specific state here if needed (e.g., health, dialogue state)
    // 如果需要，在此处添加其他特定于 NPC 的状态（例如，生命值、对话状态）
}

[Serializable]
public class NPCStatePair
{
    public string uniqueID; // Matches SavableNPC.uniqueID / 匹配 SavableNPC.uniqueID
    public NPCSaveData npcState;
}


// --- Scene Saving Structure ---
// --- 场景保存结构体 ---

[Serializable]
public class SceneSaveData
{
    // Initialize lists to prevent null reference errors if loaded data is missing one
    // 初始化列表以防止在加载的数据缺少列表时出现空引用错误
    public List<ItemStatePair> itemsState = new List<ItemStatePair>();
    public List<NPCStatePair> npcsState = new List<NPCStatePair>(); // List for NPC states / NPC 状态列表
}

// --- Structures for Overall Save Data (Optional - if saving to disk) ---
// --- 整体存档数据结构体（可选 - 如果要保存到磁盘）---

[Serializable]
public class SceneDataPair
{
    public string sceneName;
    public SceneSaveData sceneData;
}

// Central save data structure if saving multiple scenes to a file
// 如果将多个场景保存到文件中的中央存档数据结构
[Serializable]
public class SaveData
{
    public List<SceneDataPair> sceneSaveData = new List<SceneDataPair>();
    // Add other global save data here (e.g., player stats, progress manager state)
    // 在此处添加其他全局存档数据（例如，玩家统计数据、进度管理器状态）
}
