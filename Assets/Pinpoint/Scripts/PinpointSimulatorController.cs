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
    [SerializeField] private MonoBehaviour markerAnchorProviderBehaviour;
    private IPointerRayProvider _pointerRayProvider;
    private IMarkerAnchorProvider _markerAnchorProvider;

    [Header("Raycast")]
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private float fallbackDistance = 2f;
    [SerializeField] private TMP_Text saveButtonLabel;
    [SerializeField] private TMP_Text sessionStatusText;

    private readonly List<GameObject> _markers = new();
    private GameObject _selected;
    private bool _isDirty;
    private string _lastSavedAtUtc;
    private const string SaveCleanLabel = "Save";
    private const string SaveDirtyLabel = "Save*";

    private void Awake()
    {
        RefreshSaveButtonLabel();
        RefreshSessionStatusText();
        _pointerRayProvider = pointerRayProviderBehaviour as IPointerRayProvider;
        if (_pointerRayProvider == null)
            Debug.LogError("Pointer ray provider is not assigned or does not implement IPointerRayProvider.");

        _markerAnchorProvider = markerAnchorProviderBehaviour as IMarkerAnchorProvider;
        if (_markerAnchorProvider == null)
            Debug.LogError("Marker anchor provider is not assigned or does not implement IMarkerAnchorProvider.");

        if (detailsPanel != null)
        {
            detailsPanel.OnMarkerEdited = MarkDirty;
        }
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
        MarkDirty();
        RefreshSessionStatusText();
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

    private void SetSelectedMarker(PinpointMarkerModel marker)
    {
        if (_selected == marker)
            return;

        if (_selected != null)
        {
            var oldView = _selected.GetComponent<PinpointMarkerView>();
            if (oldView != null)
                oldView.SetSelected(false);
        }

        _selected = marker;

        if (_selected != null)
        {
            var newView = _selected.GetComponent<PinpointMarkerView>();
            if (newView != null)
                newView.SetSelected(true);

            if (detailsPanel != null) {
                var data = _selected != null ? _selected.GetComponent<PinpointMarkerModel>() : null;
                detailsPanel.Bind(data);
            }
        }
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
        MarkDirty();
        RefreshSessionStatusText();
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
            sessionName = "Pinpoint Session",
            lastSavedAtUtc = _lastSavedAtUtc
        };

        foreach (var marker in _markers)
        {
            if (marker == null) continue;

            var data = marker.GetComponent<PinpointMarkerModel>();
            if (data == null) continue;

            var anchor = CreateAnchorDto(marker.transform);
            session.markers.Add(new PinpointMarkerDto
            {
                markerId = data.MarkerId,
                createdAtUtc = data.CreatedAtUtc,
                updatedAtUtc = data.UpdatedAtUtc,
                title = data.Title,
                severity = (int)data.Severity,
                status = (int)data.Status,
                rawNote = data.RawNote,
                anchor = anchor,
                position = anchor.position
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
        RefreshSessionStatusText();
    }

    private void LoadSessionFromDto(PinpointSessionDto session)
    {
        if (session == null) return;

        ClearAllMarkers();
        _lastSavedAtUtc = string.IsNullOrWhiteSpace(session.lastSavedAtUtc)
            ? ""
            : PinpointTimestamp.EnsureUtcIso(session.lastSavedAtUtc);

        foreach (var markerDto in session.markers)
        {
            Pose markerPose = ResolveMarkerPose(markerDto);
            var go = Instantiate(markerPrefab, markerPose.position, markerPose.rotation);
            go.tag = "PinpointMarker";
            //go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            var data = go.GetComponent<PinpointMarkerModel>();
            if (data == null) data = go.AddComponent<PinpointMarkerModel>();

            data.LoadFromDto(markerDto);

            if (go.GetComponent<PinpointMarkerView>() == null)
                go.AddComponent<PinpointMarkerView>();

            _markers.Add(go);
        }

        DeselectCurrent();
        RefreshSessionStatusText();
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

    private MarkerAnchorDto CreateAnchorDto(Transform markerTransform)
    {
        if (_markerAnchorProvider != null)
            return _markerAnchorProvider.CreateAnchor(markerTransform);

        return MarkerAnchorDto.FromTransform(MarkerAnchorSource.Simulator, markerTransform);
    }

    private Pose ResolveMarkerPose(PinpointMarkerDto markerDto)
    {
        if (_markerAnchorProvider != null)
            return _markerAnchorProvider.ResolvePose(markerDto);

        if (markerDto.anchor != null && markerDto.anchor.HasUsablePose)
            return markerDto.anchor.ToPose();

        return new Pose(markerDto.position, Quaternion.identity);
    }

    public void SaveSession()
    {
        var session = CreateSessionDtoFromScene();
        session.lastSavedAtUtc = PinpointTimestamp.NowUtcIso();
        PinpointSessionStorage.Save(session);
        _lastSavedAtUtc = session.lastSavedAtUtc;
        MarkClean();
        RefreshSessionStatusText();
        Debug.Log("Session Saved");
    }
    public void LoadSession()
    {
        var session = PinpointSessionStorage.Load();
        LoadSessionFromDto(session);
        MarkClean();
        RefreshSessionStatusText();
        Debug.Log("Session Loaded");
    }

    public void NewSession()
    {
        ClearAllMarkers();
        _lastSavedAtUtc = "";
        MarkClean();
        RefreshSessionStatusText();
        Debug.Log("Markers Cleared");
    }

    private void MarkDirty()
    {
        if (_isDirty)
            return;

        _isDirty = true;
        RefreshSaveButtonLabel();
    }

    private void MarkClean()
    {
        if (!_isDirty)
            return;

        _isDirty = false;
        RefreshSaveButtonLabel();
    }

    private void RefreshSaveButtonLabel()
    {
        if (saveButtonLabel != null)
        {
            saveButtonLabel.text = _isDirty ? SaveDirtyLabel : SaveCleanLabel;
        }
    }

    private void RefreshSessionStatusText()
    {
        if (sessionStatusText == null)
            return;

        sessionStatusText.text =
            $"Markers: {_markers.Count} | Last saved: {PinpointTimestamp.FormatDisplay(_lastSavedAtUtc)}";
    }
}
