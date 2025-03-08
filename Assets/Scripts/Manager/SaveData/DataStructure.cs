using System;
using System.Collections.Generic;

// 用于物品状态保存
[Serializable]
public class ItemSaveData
{
    public string type; // "ItemPickup" 或 "ItemCheckUp"
    public bool isPresent; // 仅用于ItemPickup，表示是否存在
    public bool hasBeenChecked; // 仅用于ItemCheckUp，表示是否被检查
}

// 用于序列化字典
[Serializable]
public class ItemStatePair
{
    public string uniqueID;
    public ItemSaveData itemState;
}

// 用于保存场景状态，储存状态列表
[Serializable]
public class SceneSaveData
{
    public List<ItemStatePair> itemsState; // 物品状态列表
}

// 用于序列化场景保存数据
[Serializable]
public class SceneDataPair
{
    public string sceneName;
    public SceneSaveData sceneData;
}

// 中央保存数据
[Serializable]
public class SaveData
{
    public List<SceneDataPair> sceneSaveData; // 所有场景的保存数据
}