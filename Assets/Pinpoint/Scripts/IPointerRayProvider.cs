using UnityEngine;

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
    bool IsSceneActionBlockedByUi();
    bool IsTextInputActive();
}
