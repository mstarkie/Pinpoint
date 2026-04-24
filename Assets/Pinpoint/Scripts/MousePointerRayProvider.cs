using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MousePointerRayProvider : MonoBehaviour, IPointerRayProvider, IPinpointInteractionInputProvider
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private int sceneActionMouseButton = 0;
    [SerializeField] private KeyCode saveKey = KeyCode.S;
    [SerializeField] private KeyCode loadKey = KeyCode.L;
    [SerializeField] private KeyCode newSessionKey = KeyCode.N;
    [SerializeField] private KeyCode deleteSelectedKey = KeyCode.Delete;
    [SerializeField] private KeyCode alternateDeleteSelectedKey = KeyCode.Backspace;
    [SerializeField] private KeyCode exportAnalysisKey = KeyCode.E;
    [SerializeField] private KeyCode togglePlacementModeKey = KeyCode.P;
    [SerializeField] private bool blockSceneActionWhenPointerOverUi = true;

    private void Reset()
    {
        targetCamera = Camera.main;
    }

    public Ray GetPointerRay()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("MousePointerRayProvider: targetCamera is not assigned.");
            return new Ray(Vector3.zero, Vector3.forward);
        }

        return targetCamera.ScreenPointToRay(Input.mousePosition);
    }

    public bool WasSceneActionRequested()
    {
        return Input.GetMouseButtonDown(sceneActionMouseButton);
    }

    public bool WasSaveRequested()
    {
        return Input.GetKeyDown(saveKey);
    }

    public bool WasLoadRequested()
    {
        return Input.GetKeyDown(loadKey);
    }

    public bool WasNewSessionRequested()
    {
        return Input.GetKeyDown(newSessionKey);
    }

    public bool WasDeleteSelectedRequested()
    {
        return Input.GetKeyDown(deleteSelectedKey) || Input.GetKeyDown(alternateDeleteSelectedKey);
    }

    public bool WasExportAnalysisRequested()
    {
        return Input.GetKeyDown(exportAnalysisKey);
    }

    public bool WasTogglePlacementModeRequested()
    {
        return Input.GetKeyDown(togglePlacementModeKey);
    }

    public bool IsSceneActionBlockedByUi()
    {
        return blockSceneActionWhenPointerOverUi &&
               EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    public bool IsTextInputActive()
    {
        if (EventSystem.current == null)
            return false;

        var selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.GetComponent<TMP_InputField>() != null;
    }
}
