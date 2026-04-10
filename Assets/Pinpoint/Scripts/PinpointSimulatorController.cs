using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
public class PinpointSimulatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private MarkerDetailsPanel detailsPanel;
    [SerializeField] private MonoBehaviour pointerRayProviderBehaviour;
    private IPointerRayProvider _pointerRayProvider;

    [Header("Raycast")]
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private float fallbackDistance = 2f;


    private readonly List<GameObject> _markers = new();
    private GameObject _selected;

    private void Awake()
    {
        _pointerRayProvider = pointerRayProviderBehaviour as IPointerRayProvider;
        if (_pointerRayProvider == null)
            Debug.LogError("Pointer ray provider is not assigned or does not implement IPointerRayProvider.");
    }

    void Reset()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
                
            HandleLeftClick();
        }

        if (IsTypingInInputField())
            return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveSession();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSession();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            NewSession();
        }
    }

    private void HandleLeftClick()
    {
        if (mainCamera == null)
        {
            Debug.LogError("PinpointSimulatorController: mainCamera is not assigned.");
            return;
        }
        if (markerPrefab == null)
        {
            Debug.LogError("PinpointSimulatorController: markerPrefab is not assigned.");
            return;
        }

        Ray ray = _pointerRayProvider.GetPointerRay();

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask, QueryTriggerInteraction.Ignore))
        {
            // If we clicked a marker, select it; otherwise place a new marker at the hit point.
            if (hit.collider.CompareTag("PinpointMarker"))
            {
                SelectMarker(hit.collider.gameObject);
            }
            else
            {
                PlaceMarker(hit.point);
            }
        }
        else
        {
            // Fallback: place in front of camera
            //Vector3 p = mainCamera.transform.position + mainCamera.transform.forward * fallbackDistance;
            //PlaceMarker(p);
            DeselectCurrent();
        }
    }

    private void PlaceMarker(Vector3 position)
    {
        var marker = Instantiate(markerPrefab, position, Quaternion.identity);
        marker.tag = "PinpointMarker";

        var data = marker.GetComponent<PinpointMarkerModel>();
        if (data == null) data = marker.AddComponent<PinpointMarkerModel>();

        data.InitializeNew();

        if (marker.GetComponent<PinpointMarkerView>() == null)
            marker.AddComponent<PinpointMarkerView>();

        _markers.Add(marker);
        SelectMarker(marker);
    }

    private void SelectMarker(GameObject marker)
    {
        // Unhighlight old
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView oldView))
            oldView.SetSelected(false);

        _selected = marker;

        // Highlight new
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView newView))
            newView.SetSelected(true);

        var data = _selected != null ? _selected.GetComponent<PinpointMarkerModel>() : null;
        detailsPanel.Bind(data);
    }

    public void DeleteSelectedMarker()
    {
        if (_selected == null)
            return;

        _markers.Remove(_selected);
        Destroy(_selected);
        _selected = null;

        if (detailsPanel != null)
            detailsPanel.Bind(null);
    }

    private void DeselectCurrent()
    {
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView oldView))
            oldView.SetSelected(false);

        _selected = null;

        if (detailsPanel != null)
            detailsPanel.Bind(null);
    }

    private PinpointSessionDto CreateSessionDtoFromScene()
    {
        var session = new PinpointSessionDto
        {
            sessionName = "Pinpoint Session"
        };

        foreach (var marker in _markers)
        {
            if (marker == null) continue;

            var data = marker.GetComponent<PinpointMarkerModel>();
            if (data == null) continue;

            session.markers.Add(new PinpointMarkerDto
            {
                markerId = data.MarkerId,
                title = data.Title,
                severity = (int)data.Severity,
                status = (int)data.Status,
                rawNote = data.RawNote,
                position = marker.transform.position
            });
        }

        return session;
    }

    private void ClearAllMarkers()
    {
        DeselectCurrent();

        foreach (var marker in _markers)
        {
            if (marker != null)
                Destroy(marker);
        }

        _markers.Clear();
    }

    private void LoadSessionFromDto(PinpointSessionDto session)
    {
        if (session == null) return;

        ClearAllMarkers();

        foreach (var markerDto in session.markers)
        {
            var go = Instantiate(markerPrefab, markerDto.position, Quaternion.identity);
            go.tag = "PinpointMarker";
            go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            var data = go.GetComponent<PinpointMarkerModel>();
            if (data == null) data = go.AddComponent<PinpointMarkerModel>();

            data.LoadFromDto(markerDto);

            if (go.GetComponent<PinpointMarkerView>() == null)
                go.AddComponent<PinpointMarkerView>();

            _markers.Add(go);
        }

        DeselectCurrent();
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null;
    }

    public void SaveSession()
    {
        var session = CreateSessionDtoFromScene();
        PinpointSessionStorage.Save(session);
        Debug.Log("Session Saved");
    }
    public void LoadSession()
    {
        var session = PinpointSessionStorage.Load();
        LoadSessionFromDto(session);
        Debug.Log("Session Loaded");
    }

    public void NewSession()
    {
        ClearAllMarkers();
        Debug.Log("Markers Cleared");
    }
}
