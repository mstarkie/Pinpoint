using UnityEngine;

public class DraggablePanelController : MonoBehaviour
{
    [SerializeField] private RectTransform panelTransform;
    [SerializeField] private MonoBehaviour dragInputProviderBehaviour;
    [SerializeField] private bool clampToParentBounds = true;
    [SerializeField] private bool bringToFrontOnDrag = true;

    private IPanelDragInputProvider _dragInputProvider;
    private RectTransform _parentTransform;
    private Vector2 _lastPointerLocalPosition;
    private bool _isDragging;

    private void Awake()
    {
        if (panelTransform == null)
            panelTransform = transform as RectTransform;

        if (panelTransform != null)
            _parentTransform = panelTransform.parent as RectTransform;

        _dragInputProvider = dragInputProviderBehaviour as IPanelDragInputProvider;
        if (_dragInputProvider == null)
            Debug.LogError("DraggablePanelController: drag input provider is not assigned or does not implement IPanelDragInputProvider.");
    }

    private void Update()
    {
        if (_dragInputProvider == null || panelTransform == null || _parentTransform == null)
            return;

        if (_dragInputProvider.TryConsumeDragStart(out var startInput))
            BeginDrag(startInput);

        if (_isDragging && _dragInputProvider.TryGetDragUpdate(out var updateInput))
            UpdateDrag(updateInput);

        if (_dragInputProvider.TryConsumeDragEnd(out _))
            _isDragging = false;
    }

    private void BeginDrag(PanelDragInput input)
    {
        if (!TryGetPointerLocalPosition(input, out _lastPointerLocalPosition))
            return;

        _isDragging = true;

        if (bringToFrontOnDrag)
            panelTransform.SetAsLastSibling();
    }

    private void UpdateDrag(PanelDragInput input)
    {
        if (!TryGetPointerLocalPosition(input, out var pointerLocalPosition))
            return;

        Vector2 delta = pointerLocalPosition - _lastPointerLocalPosition;
        panelTransform.anchoredPosition += delta;
        _lastPointerLocalPosition = pointerLocalPosition;

        if (clampToParentBounds)
            ClampToParentBounds();
    }

    private bool TryGetPointerLocalPosition(PanelDragInput input, out Vector2 localPosition)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentTransform,
            input.ScreenPosition,
            input.EventCamera,
            out localPosition);
    }

    private void ClampToParentBounds()
    {
        Bounds panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_parentTransform, panelTransform);
        Rect parentRect = _parentTransform.rect;
        Vector2 correction = Vector2.zero;

        if (panelBounds.min.x < parentRect.xMin)
            correction.x = parentRect.xMin - panelBounds.min.x;
        else if (panelBounds.max.x > parentRect.xMax)
            correction.x = parentRect.xMax - panelBounds.max.x;

        if (panelBounds.min.y < parentRect.yMin)
            correction.y = parentRect.yMin - panelBounds.min.y;
        else if (panelBounds.max.y > parentRect.yMax)
            correction.y = parentRect.yMax - panelBounds.max.y;

        panelTransform.anchoredPosition += correction;
    }
}
