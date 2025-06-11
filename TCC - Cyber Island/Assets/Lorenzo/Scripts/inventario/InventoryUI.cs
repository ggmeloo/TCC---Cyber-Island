using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public KeyCode toggleInventoryKey = KeyCode.I;
    public Transform slotsParent; // Arraste sua grade de slots (SlotsGrid) aqui

    private InventorySlotUI[] slots;

    void Start()
    {
        // Assina o evento: Quando o InventoryManager disser que mudou, chame a nossa função UpdateUI
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged += UpdateUI;
        }

        // Pega todos os scripts de slot que estão dentro da nossa grade
        slots = slotsParent.GetComponentsInChildren<InventorySlotUI>();

        // Garante que o inventário comece fechado
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
        // Lógica para abrir e fechar o painel do inventário
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    // Esta função é chamada automaticamente pelo evento OnInventoryChanged
    void UpdateUI()
    {
        // Passa por cada slot visual
        for (int i = 0; i < slots.Length; i++)
        {
            // Verifica se existe um item correspondente nos dados do inventário
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