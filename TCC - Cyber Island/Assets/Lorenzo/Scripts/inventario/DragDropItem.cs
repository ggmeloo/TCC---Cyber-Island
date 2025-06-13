using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parentAfterDrag;
    private Image image;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        image = GetComponent<Image>();
        // O CanvasGroup deve estar no objeto PAI do slot
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (image.sprite == null || !image.enabled) { eventData.pointerDrag = null; return; }

        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag);
        // Garante que o ícone volte para o centro do slot
        transform.localPosition = Vector3.zero;
        image.raycastTarget = true;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }
}