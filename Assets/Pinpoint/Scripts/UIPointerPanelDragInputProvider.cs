using UnityEngine;
using UnityEngine.EventSystems;

public class UIPointerPanelDragInputProvider : MonoBehaviour, IPanelDragInputProvider, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private PanelDragInput _currentInput;
    private PanelDragInput _lastEndInput;
    private bool _isDragging;
    private bool _dragStarted;
    private bool _dragEnded;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _currentInput = PanelDragInput.FromPointerEvent(eventData);
        _isDragging = true;
        _dragStarted = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _currentInput = PanelDragInput.FromPointerEvent(eventData);
        _isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _lastEndInput = PanelDragInput.FromPointerEvent(eventData);
        _isDragging = false;
        _dragEnded = true;
    }

    public bool TryConsumeDragStart(out PanelDragInput input)
    {
        input = _currentInput;

        if (!_dragStarted)
            return false;

        _dragStarted = false;
        return true;
    }

    public bool TryGetDragUpdate(out PanelDragInput input)
    {
        input = _currentInput;
        return _isDragging;
    }

    public bool TryConsumeDragEnd(out PanelDragInput input)
    {
        input = _lastEndInput;

        if (!_dragEnded)
            return false;

        _dragEnded = false;
        return true;
    }
}
