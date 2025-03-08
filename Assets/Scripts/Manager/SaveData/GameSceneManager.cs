using System.Collections.Generic;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [SerializeField] public string sceneName;
    [SerializeField] private List<string> itemUniqueIDs = new List<string>();
    [SerializeField] private List<ItemTypePair> itemTypePairs = new List<ItemTypePair>();
    public Dictionary<string, string> itemTypes = new Dictionary<string, string>();

    [System.Serializable]
    public class ItemTypePair
    {
        public string uniqueID;
        public string itemType;
    }

    private void Awake()
    {
        if (itemUniqueIDs == null) itemUniqueIDs = new List<string>();
        if (itemTypes == null) itemTypes = new Dictionary<string, string>();
        if (itemTypePairs == null) itemTypePairs = new List<ItemTypePair>();

        foreach (var pair in itemTypePairs)
        {
            if (!string.IsNullOrEmpty(pair.uniqueID) && !itemTypes.ContainsKey(pair.uniqueID))
            {
                itemTypes[pair.uniqueID] = pair.itemType;
            }
        }
    }

    public SceneSaveData SaveCurrentState()
    {
        SceneSaveData sceneSaveData = new SceneSaveData { itemsState = new List<ItemStatePair>() };
        foreach (string uniqueID in itemUniqueIDs)
        {
            Item item = ItemHelper.GetItemByUniqueID(uniqueID);
            Debug.Log($"Saving {uniqueID}: item is {(item != null ? "present" : "null")}");
            ItemSaveData itemSaveData = new ItemSaveData
            {
                type = itemTypes.ContainsKey(uniqueID) ? itemTypes[uniqueID] : "Unknown"
            };

            if (itemSaveData.type == "ItemPickup")
            {
                itemSaveData.isPresent = (item != null);
                itemSaveData.hasBeenChecked = false;
                Debug.Log($"ItemPickup {uniqueID}: isPresent = {itemSaveData.isPresent}");
            }
            else if (itemSaveData.type == "ItemCheckUp")
            {
                itemSaveData.isPresent = true;
                ItemCheckUp checkUp = item as ItemCheckUp;
                itemSaveData.hasBeenChecked = (checkUp != null && checkUp.hasBeenChecked);
            }
            else
            {
                Debug.LogWarning($"Unknown item type for uniqueID: {uniqueID}");
                continue;
            }

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