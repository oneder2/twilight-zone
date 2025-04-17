using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : Singleton<InventoryUI>
{
    public GameObject inventoryPanel;        // Package UI panel
    public Transform itemsParent;            // Parent group of items
    public GameObject itemSlotPrefab;        // Prefab Of items block
    private Inventory inventory;

    void Start()
    {
        inventory = Inventory.Instance;      // Access Inventory with Singeloton mode
        inventoryPanel.SetActive(false);     // Invisable by default
        EventManager.Instance.AddListener<ItemPickedUpEvent>(data =>
            {
                Debug.Log($"Player picked up an item: {data.ItemName}");
                UpdateInventoryUI();
            });
    }

    void OnDestroy()
    {
        EventManager.Instance.RemoveListener<ItemPickedUpEvent>(data =>
            {
                Debug.Log($"Player picked up an item: {data.ItemName}");
                UpdateInventoryUI();
            });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))     // Press "I" to switch visibility of package
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

    public void UpdateInventoryUI()
    {
        // Clear current blocks
        foreach (Transform child in itemsParent)
        {
            Destroy(child.gameObject);
        }

        // Create block for each items
        foreach (var itemData in inventory.GetItemDatas())
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemsParent);
            Image iconImage = slot.GetComponentInChildren<Image>();
            
            Debug.Log("Item: " + itemData + ", Icon: " + (itemData.icon != null ? itemData.icon.name : "null"));
            iconImage.sprite = itemData.icon;
        }
    }    
}