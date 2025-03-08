// using System.Collections.Generic;
// using UnityEngine;

// public class SceneStateManager : MonoBehaviour
// {
//     private Dictionary<ItemPickup, bool> itemAvaiableDict = new Dictionary<ItemPickup, bool>();

//     private void OnEnable()
//     {
//         EventHandler.BeforeSceneUnloadEvent += OnBeforeSceneUnloadEvent();
//         EventHandler.AfterSceneUnloadEvent += OnAfterSceneUnloadEvent();
//         EventHandler.AfterSceneUnloadEvent += UpdateUIEvent();
//     }

//     private void OnDisable()
//     {
//         EventHandler.BeforeSceneUnloadEvent -= OnBeforeSceneUnloadEvent();
//         EventHandler.AfterSceneUnloadEvent -= OnAfterSceneUnloadEvent();
//         EventHandler.AfterSceneUnloadEvent -= UpdateUIEvent();
//     }

//     private void OnBeforeSceneUnloadEvent()
//     {
//         foreach ( var item in FindAnyObjectByType<ItemPickup>())
//         {
//             if (!itemAvaiableDict.ContainsKey(item.itemName))
//                 // 不存在在物品字典中， 添加
//                 itemAvaiableDict.Add(item.itemName, true);
//         }
//     }

//     private void OnAfterSceneUnloadEvent()
//     {
//         // 遍历场景物品
//         foreach ( var item in FindAnyObjectByType<Item>())
//         {
//             if (!itemAvaiableDict.ContainsKey(item.itemName))
//                 // 不存在在物品字典中， 添加
//                 itemAvaiableDict.Add(item.itemName, true);
//             else
//                 // 存在在物品字典中，更新状态
//                 item.gameObject.SetActive(itemAvaiableDict[item.itemName]);
//         }
//     }

//     private void UpdateUIEvent(Item item, int arg2)
//     {
//         if (item != null)
//         {
//             itemAvaiableDict[item] = false;
//         }
//     }
// }