using UnityEngine;

public class ControladorInventario : MonoBehaviour
{
    public GameObject painelInventario;
    public Transform slotsParentInventario; // Arraste a grade de slots do seu inventário principal

    private InventorySlotUI[] inventorySlotsUI;
    private bool inventarioAberto = false;

    void Start()
    {
        painelInventario.SetActive(false);

        if (slotsParentInventario != null)
        {
            inventorySlotsUI = slotsParentInventario.GetComponentsInChildren<InventorySlotUI>();
        }

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged += UpdateInventoryUI;
        }
        if (SlotBarManager.instance != null)
        {
            SlotBarManager.instance.OnHotbarChanged += SlotBarManager.instance.UpdateHotbarUI;
        }

        UpdateInventoryUI();
    }

    void OnDestroy()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged -= UpdateInventoryUI;
        if (SlotBarManager.instance != null)
            SlotBarManager.instance.OnHotbarChanged -= SlotBarManager.instance.UpdateHotbarUI;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            inventarioAberto = !inventarioAberto;
            painelInventario.SetActive(inventarioAberto);

            if (EstadoJogador.instance != null)
            {
                if (inventarioAberto) EstadoJogador.instance.AtivarModoUI();
                else EstadoJogador.instance.DesativarModoUI();
            }
        }
    }

    void UpdateInventoryUI()
    {
        if (inventorySlotsUI == null || InventoryManager.instance == null) return;
        for (int i = 0; i < inventorySlotsUI.Length; i++)
        {
            if (i < InventoryManager.instance.inventorySlots.Count && InventoryManager.instance.inventorySlots[i] != null)
            {
                inventorySlotsUI[i].AddItem(InventoryManager.instance.inventorySlots[i]);
            }
            else
            {
                inventorySlotsUI[i].ClearSlot();
            }
        }
    }
}