using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SlotBarManager : MonoBehaviour
{
    // <<< MODIFICADO: SlotData agora copia os dados em vez de guardar a referência >>>
    public class SlotData
    {
        // Agora armazenamos os dados diretamente, não o componente.
        public SpecificItemType identifier;
        public Sprite icon;
        public GameObject prefab;
        public int quantity;

        // O construtor agora copia os dados do CollectibleItemInfo.
        public SlotData(CollectibleItemInfo info)
        {
            this.identifier = info.itemIdentifier;
            this.icon = info.itemIcon;
            this.prefab = info.itemPrefab;
            this.quantity = 1; // Começa com 1
        }
    }

    public static SlotBarManager instance;

    [Header("UI - Arraste do Canvas")]
    public List<Image> slotIconImages;
    public List<TextMeshProUGUI> slotQuantityTexts;
    public List<GameObject> slotObjects;
    public GameObject selectionFrame;

    [Header("World References")]
    public Transform playerTransform;

    private List<SlotData> itemsInSlots;
    private int selectedSlot = -1;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        itemsInSlots = new List<SlotData>(new SlotData[slotObjects.Count]);
        for (int i = 0; i < slotObjects.Count; i++)
        {
            UpdateSlotUI(i);
        }
        UpdateSelectionVisuals();
    }

    public bool AddItem(CollectibleItemInfo itemInfo)
    {
        // <<< MODIFICADO: A verificação agora usa o 'identifier' que copiamos >>>
        for (int i = 0; i < itemsInSlots.Count; i++)
        {
            if (itemsInSlots[i] != null && itemsInSlots[i].identifier == itemInfo.itemIdentifier)
            {
                itemsInSlots[i].quantity++;
                UpdateSlotUI(i);
                Debug.Log($"Empilhou {itemInfo.name}. Nova quantidade: {itemsInSlots[i].quantity}");
                return true;
            }
        }

        for (int i = 0; i < itemsInSlots.Count; i++)
        {
            if (itemsInSlots[i] == null)
            {
                itemsInSlots[i] = new SlotData(itemInfo); // O construtor faz a cópia dos dados
                UpdateSlotUI(i);
                Debug.Log($"Adicionou {itemInfo.name} ao slot {i + 1}");
                return true;
            }
        }

        Debug.Log("Barra de slots está cheia!");
        return false;
    }

    public void UseSelectedItem()
    {
        if (selectedSlot == -1 || itemsInSlots[selectedSlot] == null)
        {
            return;
        }

        itemsInSlots[selectedSlot].quantity--;

        // <<< MODIFICADO: Usa o nome do prefab para o log, que é seguro >>>
        Debug.Log($"Usou 1 {itemsInSlots[selectedSlot].prefab.name}. Restam: {itemsInSlots[selectedSlot].quantity}");

        if (itemsInSlots[selectedSlot].quantity <= 0)
        {
            itemsInSlots[selectedSlot] = null;
        }

        UpdateSlotUI(selectedSlot);
    }

    public void DropSelectedItem()
    {
        if (selectedSlot == -1 || itemsInSlots[selectedSlot] == null)
        {
            return;
        }
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform não foi atribuído no SlotBarManager!");
            return;
        }

        SlotData dataToDrop = itemsInSlots[selectedSlot];

        // <<< MODIFICADO: A verificação e instanciação agora usam os dados copiados >>>
        if (dataToDrop.prefab == null)
        {
            Debug.LogError($"O item do tipo {dataToDrop.identifier} não tem um 'itemPrefab' atribuído.");
            return;
        }

        Vector3 spawnPosition = playerTransform.position + playerTransform.forward * 1.5f + playerTransform.up * 0.5f;

        // Instancia usando a referência segura ao prefab
        GameObject droppedItemObject = Instantiate(dataToDrop.prefab, spawnPosition, Quaternion.identity);

        // <<< CORRIGIDO: O log agora usa o nome do prefab, que não foi destruído >>>
        Debug.Log($"Dropeou 1 {dataToDrop.prefab.name}.");

        Rigidbody rb = droppedItemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(playerTransform.forward * 2f, ForceMode.Impulse);
        }

        dataToDrop.quantity--;
        if (dataToDrop.quantity <= 0)
        {
            itemsInSlots[selectedSlot] = null;
        }

        UpdateSlotUI(selectedSlot);
    }

    private void UpdateSlotUI(int index)
    {
        if (itemsInSlots[index] != null)
        {
            // <<< MODIFICADO: Usa os dados copiados e seguros >>>
            slotIconImages[index].sprite = itemsInSlots[index].icon;
            slotIconImages[index].enabled = true;
            slotQuantityTexts[index].text = itemsInSlots[index].quantity.ToString();
            slotQuantityTexts[index].enabled = true;
        }
        else
        {
            slotIconImages[index].enabled = false;
            slotQuantityTexts[index].enabled = false;
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotObjects.Count) return;
        selectedSlot = index;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        if (selectedSlot != -1)
        {
            selectionFrame.SetActive(true);
            selectionFrame.transform.position = slotObjects[selectedSlot].transform.position;
        }
        else
        {
            selectionFrame.SetActive(false);
        }
    }
}