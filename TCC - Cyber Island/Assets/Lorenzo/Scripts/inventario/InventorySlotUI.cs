using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI quantityText;

    // Coloca as informações do item neste slot visual
    public void AddItem(InventoryManager.SlotData newItem)
    {
        icon.sprite = newItem.icon;
        icon.enabled = true;
        quantityText.text = newItem.quantity.ToString();
        quantityText.enabled = true;
    }

    // Limpa o slot, escondendo tudo
    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
        quantityText.text = "";
        quantityText.enabled = false;
    }
}