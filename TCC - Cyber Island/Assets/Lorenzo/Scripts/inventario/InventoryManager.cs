using UnityEngine;
using System.Collections.Generic;
using System; // Necess�rio para o evento 'Action'

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    // A classe de dados que j� usamos, perfeita para o invent�rio
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

    // Evento que avisa a UI que o invent�rio mudou
    public event Action OnInventoryChanged;

    public List<SlotData> inventorySlots = new List<SlotData>();
    public int maxSlots = 24; // Defina aqui quantos slots seu invent�rio tem

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Preenche o invent�rio com espa�os vazios (null)
        for (int i = 0; i < maxSlots; i++)
        {
            inventorySlots.Add(null);
        }
    }

    public bool AddItem(CollectibleItemInfo itemInfo)
    {
        // 1. Tentar empilhar em um slot existente
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i] != null && inventorySlots[i].identifier == itemInfo.itemIdentifier)
            {
                inventorySlots[i].quantity++;
                OnInventoryChanged?.Invoke(); // Dispara o evento: "Ei UI, atualize-se!"
                return true;
            }
        }

        // 2. Procura um slot vazio
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = new SlotData(itemInfo);
                OnInventoryChanged?.Invoke(); // Dispara o evento
                return true;
            }
        }

        Debug.Log("Invent�rio est� cheio!");
        return false;
    }
}