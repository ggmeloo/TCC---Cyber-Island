using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public KeyCode toggleInventoryKey = KeyCode.I;
    public Transform slotsParent; // Arraste sua grade de slots (SlotsGrid) aqui

    private InventorySlotUI[] slots;

    void Start()
    {
        // Assina o evento: Quando o InventoryManager disser que mudou, chame a nossa fun��o UpdateUI
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged += UpdateUI;
        }

        // Pega todos os scripts de slot que est�o dentro da nossa grade
        slots = slotsParent.GetComponentsInChildren<InventorySlotUI>();

        // Garante que o invent�rio comece fechado
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Importante: Cancela a assinatura do evento para evitar erros quando a cena for descarregada
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged -= UpdateUI;
        }
    }

    void Update()
    {
        // L�gica para abrir e fechar o painel do invent�rio
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    // Esta fun��o � chamada automaticamente pelo evento OnInventoryChanged
    void UpdateUI()
    {
        // Passa por cada slot visual
        for (int i = 0; i < slots.Length; i++)
        {
            // Verifica se existe um item correspondente nos dados do invent�rio
            if (i < InventoryManager.instance.inventorySlots.Count && InventoryManager.instance.inventorySlots[i] != null)
            {
                // Manda o slot visual exibir o item
                slots[i].AddItem(InventoryManager.instance.inventorySlots[i]);
            }
            else
            {
                // Manda o slot visual se limpar
                slots[i].ClearSlot();
            }
        }
    }
}