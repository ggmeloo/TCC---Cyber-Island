using UnityEngine;
using UnityEngine.EventSystems;

public class SlotDropTarget : MonoBehaviour, IDropHandler
{
    public enum SlotType { INVENTORY, HOTBAR }
    public SlotType slotType;
    public int slotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        DragDropItem draggedItem = droppedObject.GetComponent<DragDropItem>();

        if (draggedItem == null) return;

        SlotDropTarget originSlot = draggedItem.parentAfterDrag.GetComponent<SlotDropTarget>();

        if (originSlot != null && originSlot != this)
        {
            SwapItems(originSlot, this);
        }
    }

    private void SwapItems(SlotDropTarget origin, SlotDropTarget destination)
    {
        InventoryManager.SlotData originData = null;
        if (origin.slotType == SlotType.INVENTORY)
            originData = InventoryManager.instance.RemoveItemAt(origin.slotIndex);
        else
            originData = SlotBarManager.instance.RemoveItemAt(origin.slotIndex);

        InventoryManager.SlotData destinationData = null;
        if (destination.slotType == SlotType.INVENTORY)
            destinationData = InventoryManager.instance.RemoveItemAt(destination.slotIndex);
        else
            destinationData = SlotBarManager.instance.RemoveItemAt(destination.slotIndex);

        if (originData != null)
        {
            if (destination.slotType == SlotType.INVENTORY)
                InventoryManager.instance.AddItemAt(originData, destination.slotIndex);
            else
                SlotBarManager.instance.AddItemAt(originData, destination.slotIndex);
        }

        if (destinationData != null)
        {
            if (origin.slotType == SlotType.INVENTORY)
                InventoryManager.instance.AddItemAt(destinationData, origin.slotIndex);
            else
                SlotBarManager.instance.AddItemAt(destinationData, origin.slotIndex);
        }
    }
}