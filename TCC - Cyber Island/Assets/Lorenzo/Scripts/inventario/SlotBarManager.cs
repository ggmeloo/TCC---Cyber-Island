using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class SlotBarManager : MonoBehaviour
{
    public static SlotBarManager instance;
    public event Action OnHotbarChanged;

    [Header("UI - Arraste do Canvas")]
    public List<Image> slotIconImages;
    public List<TextMeshProUGUI> slotQuantityTexts;
    public List<GameObject> slotObjects;
    public GameObject selectionFrame;

    [Header("World References")]
    public Transform playerTransform;

    public List<InventoryManager.SlotData> hotbarSlots;
    private int selectedSlot = -1;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        hotbarSlots = new List<InventoryManager.SlotData>(new InventoryManager.SlotData[5]); // 5 slots na hotbar
    }

    void Start()
    {
        UpdateHotbarUI();
        UpdateSelectionVisuals();
    }

    public void UpdateHotbarUI()
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            if (hotbarSlots[i] != null)
            {
                slotIconImages[i].sprite = hotbarSlots[i].icon;
                slotIconImages[i].enabled = true;
                slotQuantityTexts[i].text = hotbarSlots[i].quantity.ToString();
                slotQuantityTexts[i].enabled = true;
            }
            else
            {
                slotIconImages[i].enabled = false;
                slotQuantityTexts[i].enabled = false;
            }
        }
    }

    // Funções para Drag-and-Drop
    public InventoryManager.SlotData RemoveItemAt(int index)
    {
        if (index < 0 || index >= hotbarSlots.Count || hotbarSlots[index] == null) return null;
        InventoryManager.SlotData itemData = hotbarSlots[index];
        hotbarSlots[index] = null;
        OnHotbarChanged?.Invoke();
        return itemData;
    }

    public void AddItemAt(InventoryManager.SlotData itemData, int index)
    {
        if (index < 0 || index >= hotbarSlots.Count) return;
        hotbarSlots[index] = itemData;
        OnHotbarChanged?.Invoke();
    }

    // Funções de uso e drop da hotbar
    public void UseSelectedItem() { /* ...código existente... */ }
    public void DropSelectedItem() { /* ...código existente... */ }
    public void SelectSlot(int index) { /* ...código existente... */ }
    private void UpdateSelectionVisuals() { /* ...código existente... */ }
}