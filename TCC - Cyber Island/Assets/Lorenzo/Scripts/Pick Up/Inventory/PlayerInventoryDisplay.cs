using UnityEngine;
using System.Collections.Generic;

public class PlayerInventoryDisplay : MonoBehaviour
{
    [Header("Inventory Slot Transforms (Assign in Inspector)")] // Transforms dos Slots de Inventário (Atribuir no Inspector)
    public Transform swordSlotTransform;
    public Transform coconutSlotTransform;
    public Transform customItem3SlotTransform; // Atribua o Transform para o slot 3
    public Transform customItem4SlotTransform; // Atribua o Transform para o slot 4
    public Transform customItem5SlotTransform; // Atribua o Transform para o slot 5

    // Armazena as instâncias reais dos GameObjects atualmente nos slots
    private Dictionary<SpecificItemType, GameObject> _itemsInSlots = new Dictionary<SpecificItemType, GameObject>();
    // Mapeia tipos de item para seus transforms de slot designados para acesso mais fácil
    private Dictionary<SpecificItemType, Transform> _slotTransformsMap;

    void Awake()
    {
        // Inicializa o mapa
        _slotTransformsMap = new Dictionary<SpecificItemType, Transform>()
        {
            { SpecificItemType.Sword, swordSlotTransform },
            { SpecificItemType.GreenCoconut, coconutSlotTransform },
            { SpecificItemType.CustomItem3, customItem3SlotTransform },
            { SpecificItemType.CustomItem4, customItem4SlotTransform },
            { SpecificItemType.CustomItem5, customItem5SlotTransform }
        };
    }

    /// <summary>
    /// Tenta adicionar um GameObject de item coletado ao seu slot visual designado.
    /// </summary>
    /// <param name="itemObject">O GameObject do item coletado do mundo.</param>
    /// <param name="itemType">O SpecificItemType do item.</param>
    /// <returns>Verdadeiro se o item foi adicionado com sucesso a um slot, falso caso contrário.</returns>
    public bool AddItemToDesignatedSlot(GameObject itemObject, SpecificItemType itemType)
    {
        if (itemObject == null)
        {
            Debug.LogError("ItemObject é nulo, não pode adicionar ao slot.");
            return false;
        }

        if (!_slotTransformsMap.TryGetValue(itemType, out Transform targetSlot) || targetSlot == null)
        {
            Debug.LogWarning($"Nenhum slot de exibição de inventário definido ou atribuído para o tipo de item: {itemType}. Item: {itemObject.name}");
            return false;
        }

        if (_itemsInSlots.ContainsKey(itemType) && _itemsInSlots[itemType] != null)
        {
            Debug.Log($"Slot para {itemType} já está ocupado por {_itemsInSlots[itemType].name}. Não é possível adicionar {itemObject.name}.");
            // Opcional: Lidar com empilhamento ou substituição aqui, se desejado
            return false;
        }

        // Define o item como filho do slot, desativa sua física e o posiciona.
        itemObject.transform.SetParent(targetSlot);
        itemObject.transform.localPosition = Vector3.zero;
        // Você pode querer rotações específicas para itens em slots:
        // itemObject.transform.localRotation = Quaternion.Euler(desiredX, desiredY, desiredZ);
        itemObject.transform.localRotation = Quaternion.identity; // Padrão

        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        Collider col = itemObject.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // Desativa o colisor do mundo uma vez no slot do inventário
        }

        // Armazena a referência
        _itemsInSlots[itemType] = itemObject;
        itemObject.SetActive(true); // Garante que esteja visível

        Debug.Log($"{itemObject.name} (Tipo: {itemType}) adicionado ao seu slot de exibição de inventário.");
        return true;
    }

    /// <summary>
    /// Verifica se um tipo de item específico está atualmente em um slot.
    /// </summary>
    public bool HasItem(SpecificItemType itemType)
    {
        return _itemsInSlots.ContainsKey(itemType) && _itemsInSlots[itemType] != null;
    }

    /// <summary>
    /// Remove um item de seu slot e opcionalmente reativa sua física para soltá-lo.
    /// Isso pode ser usado se você implementar um recurso de "soltar do slot de inventário".
    /// </summary>
    public GameObject RemoveItemFromSlot(SpecificItemType itemType, bool enablePhysicsForDrop = false)
    {
        if (_itemsInSlots.TryGetValue(itemType, out GameObject itemInSlot) && itemInSlot != null)
        {
            _itemsInSlots.Remove(itemType);
            itemInSlot.transform.SetParent(null); // Remove o parentesco

            if (enablePhysicsForDrop)
            {
                Rigidbody rb = itemInSlot.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                }
                Collider col = itemInSlot.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                }
            }
            Debug.Log($"{itemInSlot.name} removido do slot para {itemType}.");
            return itemInSlot;
        }
        return null;
    }
}