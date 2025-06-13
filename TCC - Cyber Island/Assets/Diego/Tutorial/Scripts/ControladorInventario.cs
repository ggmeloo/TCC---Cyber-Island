using UnityEngine;
using System.Collections.Generic; // <<< ADICIONE ESTA LINHA

public class ControladorInventario : MonoBehaviour
{
    public GameObject painelInventario;

    // <<< NOVA REFER�NCIA: Arraste a sua grade de slots (SlotsGrid) aqui >>>
    public Transform slotsParent;

    // <<< NOVO: Array para guardar os scripts de cada slot individual >>>
    private InventorySlotUI[] slots;

    private bool inventarioAberto = false;

    void Start()
    {
        painelInventario.SetActive(false);

        // <<< NOVO: Pega todos os slots e assina o evento de atualiza��o >>>
        if (slotsParent != null)
        {
            slots = slotsParent.GetComponentsInChildren<InventorySlotUI>();
        }
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged += AtualizarInventarioUI;
        }
    }

    // <<< NOVO: Garante que a assinatura do evento seja removida >>>
    void OnDestroy()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OnInventoryChanged -= AtualizarInventarioUI;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // A l�gica de abrir/fechar continua a mesma, mas agora tamb�m atualizamos a UI ao abrir
            if (inventarioAberto) // Se j� est� aberto, fecha
            {
                FecharInventario();
            }
            else // Se est� fechado, abre
            {
                AbrirInventario();
            }
        }
    }

    void AbrirInventario()
    {
        inventarioAberto = true;
        painelInventario.SetActive(true);
        // N�o precisamos mais do EstadoJogador se este script for o �nico a controlar a UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AtualizarInventarioUI(); // <<< NOVO: Atualiza a UI sempre que o invent�rio � aberto
    }

    void FecharInventario()
    {
        inventarioAberto = false;
        painelInventario.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // <<< NOVA FUN��O: O cora��o da atualiza��o visual >>>
    void AtualizarInventarioUI()
    {
        if (slots == null) return;

        // Passa por cada slot visual da UI
        for (int i = 0; i < slots.Length; i++)
        {
            // Verifica se existe um item correspondente nos dados do InventoryManager
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