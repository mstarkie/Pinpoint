using UnityEngine;

public enum PinpointInteractionMode
{
    Select = 0,
    PlaceMarker = 1
}

public interface IPointerRayProvider
{
    Ray GetPointerRay();
}

public interface IPinpointInteractionInputProvider
{
    bool WasSceneActionRequested();
    bool WasSaveRequested();
    bool WasLoadRequested();
    bool WasNewSessionRequested();
    bool WasDeleteSelectedRequested();
    bool WasExportAnalysisRequested();
    bool WasTogglePlacementModeRequested();
    bool IsSceneActionBlockedByUi();
    bool IsTextInputActive();
}
