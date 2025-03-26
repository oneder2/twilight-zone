using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;        // 背包UI面板
    public Transform itemsParent;            // 物品槽的父对象（建议使用GridLayoutGroup）
    public GameObject itemSlotPrefab;        // 物品槽预制体（包含Image和Button）
    private Inventory inventory;
    private ItemData selectedItemData;
    private Button lastSelectedButton;       // 用于高亮上一次选中的按钮

    void Start()
    {
        inventory = Inventory.Instance;      // 使用单例获取Inventory
        inventoryPanel.SetActive(false);     // 默认隐藏
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))     // 按“I”键切换背包
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        if (inventoryPanel.activeSelf)
        {
            UpdateInventoryUI();
        }
    }

    void UpdateInventoryUI()
    {
        // 清空现有槽
        foreach (Transform child in itemsParent)
        {
            Destroy(child.gameObject);
        }

        // 为每个物品创建槽
        foreach (var itemData in inventory.GetItemDatas())
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemsParent);
            Image iconImage = slot.GetComponentInChildren<Image>();
            Text nameText = slot.GetComponentInChildren<Text>(); // 假设预制体有Text组件
            Button slotButton = slot.GetComponent<Button>();

            iconImage.sprite = itemData.icon;
            if (nameText != null) nameText.text = itemData.itemName;
            slotButton.onClick.AddListener(() => SelectItem(itemData, slotButton));
        }
    }

    void SelectItem(ItemData itemData, Button button)
    {
        selectedItemData = itemData;
        Debug.Log("Selected: " + itemData.itemName);

        // 高亮当前选中的按钮
        if (lastSelectedButton != null)
        {
            lastSelectedButton.image.color = Color.white; // 重置上一个按钮颜色
        }
        button.image.color = Color.yellow; // 高亮当前按钮
        lastSelectedButton = button;
    }

    public ItemData GetSelectedItemData()
    {
        return selectedItemData;
    }

    public void ClearSelection()
    {
        selectedItemData = null;
        if (lastSelectedButton != null)
        {
            lastSelectedButton.image.color = Color.white;
            lastSelectedButton = null;
        }
    }
}