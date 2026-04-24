using UnityEngine;
using UnityEngine.EventSystems;

public readonly struct PanelDragInput
{
    public PanelDragInput(Vector2 screenPosition, Camera eventCamera)
    {
        ScreenPosition = screenPosition;
        EventCamera = eventCamera;
    }

    public Vector2 ScreenPosition { get; }
    public Camera EventCamera { get; }

    public static PanelDragInput FromPointerEvent(PointerEventData eventData)
    {
        return new PanelDragInput(eventData.position, eventData.pressEventCamera);
    }
}

public interface IPanelDragInputProvider
{
    bool TryConsumeDragStart(out PanelDragInput input);
    bool TryGetDragUpdate(out PanelDragInput input);
    bool TryConsumeDragEnd(out PanelDragInput input);
}
