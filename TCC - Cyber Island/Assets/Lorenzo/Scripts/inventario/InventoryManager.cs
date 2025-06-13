using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    public class SlotData
    {
        public SpecificItemType identifier;
        public Sprite icon;
        public GameObject prefab;
        public int quantity;

        public SlotData(CollectibleItemInfo info)
        {
            this.identifier = info.itemIdentifier;
            this.icon = info.itemIcon;
            this.prefab = info.itemPrefab;
            this.quantity = 1;
        }
    }

    public event Action OnInventoryChanged;

    public List<SlotData> inventorySlots = new List<SlotData>();
    public int maxSlots = 24; // Ajuste para o tamanho do seu inventário principal

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < maxSlots; i++)
        {
            inventorySlots.Add(null);
        }
    }

    public bool AddItem(CollectibleItemInfo itemInfo)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].identifier == itemInfo.itemIdentifier)
            {
                inventorySlots[i].quantity++;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = new SlotData(itemInfo);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // Funções para Drag-and-Drop
    public SlotData RemoveItemAt(int index)
    {
        if (index < 0 || index >= inventorySlots.Count || inventorySlots[index] == null) return null;
        SlotData itemData = inventorySlots[index];
        inventorySlots[index] = null;
        OnInventoryChanged?.Invoke();
        return itemData;
    }

    public void AddItemAt(SlotData itemData, int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;
        inventorySlots[index] = itemData;
        OnInventoryChanged?.Invoke();
    }
}