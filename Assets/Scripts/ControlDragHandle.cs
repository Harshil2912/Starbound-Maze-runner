using UnityEngine;
using UnityEngine.EventSystems;

public class ControlDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ControlsLayoutManager layoutManager;
    public ControlsLayoutManager.ControlId controlId;

    RectTransform targetRect;
    bool dragging;

    void Awake()
    {
        targetRect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!ControlsLayoutManager.IsEditingLayout || layoutManager == null) return;
        layoutManager.SelectControl(controlId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!ControlsLayoutManager.IsEditingLayout || layoutManager == null) return;
        dragging = true;
        layoutManager.SelectControl(controlId);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || !ControlsLayoutManager.IsEditingLayout || layoutManager == null) return;
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        layoutManager.MoveControlByScreenDelta(controlId, eventData.delta, eventData.pressEventCamera);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;
        if (layoutManager != null) layoutManager.SaveLayout();
    }
}

