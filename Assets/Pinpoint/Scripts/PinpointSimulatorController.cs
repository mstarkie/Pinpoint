using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PinpointSimulatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private MarkerDetailsPanel detailsPanel;
    [SerializeField] private MonoBehaviour pointerRayProviderBehaviour;
    [SerializeField] private MonoBehaviour interactionInputProviderBehaviour;
    [SerializeField] private MonoBehaviour markerAnchorProviderBehaviour;
    private IPointerRayProvider _pointerRayProvider;
    private IPinpointInteractionInputProvider _interactionInputProvider;
    private IMarkerAnchorProvider _markerAnchorProvider;

    [Header("Raycast")]
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private float fallbackDistance = 2f;
    [SerializeField] private TMP_Text saveButtonLabel;
    [SerializeField] private TMP_Text sessionStatusText;

    [Header("Interaction")]
    [SerializeField] private PinpointInteractionMode interactionMode = PinpointInteractionMode.Select;
    [SerializeField] private bool returnToSelectAfterPlacement = true;
    [SerializeField] private Button selectModeButton;
    [SerializeField] private Button placeModeButton;

    [Header("Placement Preview")]
    [SerializeField] private bool showPlacementPreview = true;
    [SerializeField] private float placementPreviewSize = 0.14f;
    [SerializeField] private float placementPreviewSurfaceOffset = 0.01f;
    [SerializeField] private Color placementPreviewColor = new(0.1f, 1f, 0.35f, 0.65f);

    private readonly List<GameObject> _markers = new();
    private GameObject _selected;
    private GameObject _placementPreview;
    private Renderer _placementPreviewRenderer;
    private Material _placementPreviewMaterial;
    private ColorBlock _selectModeButtonDefaultColors;
    private ColorBlock _placeModeButtonDefaultColors;
    private bool _modeButtonColorsCaptured;
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

        _interactionInputProvider = interactionInputProviderBehaviour as IPinpointInteractionInputProvider;
        if (_interactionInputProvider == null)
            _interactionInputProvider = pointerRayProviderBehaviour as IPinpointInteractionInputProvider;
        if (_interactionInputProvider == null)
            Debug.LogError("Interaction input provider is not assigned or does not implement IPinpointInteractionInputProvider.");

        _markerAnchorProvider = markerAnchorProviderBehaviour as IMarkerAnchorProvider;
        if (_markerAnchorProvider == null)
            Debug.LogError("Marker anchor provider is not assigned or does not implement IMarkerAnchorProvider.");

        if (detailsPanel != null)
        {
            detailsPanel.OnMarkerEdited = MarkDirty;
        }

        CaptureModeButtonColors();
        RefreshModeButtonColors();
    }

    void Reset()
    {
        mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (_placementPreview != null)
            Destroy(_placementPreview);

        if (_placementPreviewMaterial != null)
            Destroy(_placementPreviewMaterial);
    }

    void Update()
    {
        if (_interactionInputProvider == null)
            return;

        bool textInputActive = _interactionInputProvider.IsTextInputActive();

        if (!textInputActive && _interactionInputProvider.WasTogglePlacementModeRequested())
            ToggleInteractionMode();

        UpdatePlacementPreview();

        if (_interactionInputProvider.WasSceneActionRequested())
        {
            if (!_interactionInputProvider.IsSceneActionBlockedByUi())
                HandleSceneAction();
        }

        if (textInputActive)
            return;

        if (_interactionInputProvider.WasSaveRequested())
            SaveSession();

        if (_interactionInputProvider.WasLoadRequested())
            LoadSession();

        if (_interactionInputProvider.WasNewSessionRequested())
            NewSession();

        if (_interactionInputProvider.WasDeleteSelectedRequested())
            DeleteSelectedMarker();

        if (_interactionInputProvider.WasExportAnalysisRequested())
            ExportAnalysisJson();
    }

    private void HandleSceneAction()
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

        if (interactionMode == PinpointInteractionMode.PlaceMarker)
            HandlePlaceMarkerAction();
        else
            HandleSelectAction();
    }

    private void HandleSelectAction()
    {
        if (TryRaycastScene(out RaycastHit hit, false) && hit.collider.CompareTag("PinpointMarker"))
        {
            SelectMarker(hit.collider.gameObject);
            return;
        }

        DeselectCurrent();
    }

    private void HandlePlaceMarkerAction()
    {
        if (!TryRaycastScene(out RaycastHit hit, true))
        {
            // Fallback: place in front of camera
            //Vector3 p = mainCamera.transform.position + mainCamera.transform.forward * fallbackDistance;
            //PlaceMarker(p);
            DeselectCurrent();
            return;
        }

        PlaceMarker(hit.point);

        if (returnToSelectAfterPlacement)
            SetInteractionMode(PinpointInteractionMode.Select);
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

    public void ToggleInteractionMode()
    {
        SetInteractionMode(interactionMode == PinpointInteractionMode.PlaceMarker
            ? PinpointInteractionMode.Select
            : PinpointInteractionMode.PlaceMarker);
    }

    public void SetSelectMode()
    {
        SetInteractionMode(PinpointInteractionMode.Select);
    }

    public void SetPlaceMarkerMode()
    {
        SetInteractionMode(PinpointInteractionMode.PlaceMarker);
    }

    private void SetInteractionMode(PinpointInteractionMode mode)
    {
        if (interactionMode == mode)
            return;

        interactionMode = mode;
        RefreshSessionStatusText();
        RefreshModeButtonColors();
        UpdatePlacementPreview();
    }

    private void CaptureModeButtonColors()
    {
        if (_modeButtonColorsCaptured || selectModeButton == null || placeModeButton == null)
            return;

        _selectModeButtonDefaultColors = selectModeButton.colors;
        _placeModeButtonDefaultColors = placeModeButton.colors;
        _modeButtonColorsCaptured = true;
    }

    private void RefreshModeButtonColors()
    {
        CaptureModeButtonColors();

        if (!_modeButtonColorsCaptured)
            return;

        if (interactionMode == PinpointInteractionMode.PlaceMarker)
        {
            selectModeButton.colors = _placeModeButtonDefaultColors;
            placeModeButton.colors = _selectModeButtonDefaultColors;
        }
        else
        {
            selectModeButton.colors = _selectModeButtonDefaultColors;
            placeModeButton.colors = _placeModeButtonDefaultColors;
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

    private bool TryRaycastScene(out RaycastHit hit, bool ignoreMarkers)
    {
        hit = default;

        if (_pointerRayProvider == null)
            return false;

        Ray ray = _pointerRayProvider.GetPointerRay();

        if (!ignoreMarkers)
            return Physics.Raycast(ray, out hit, 100f, placementMask, QueryTriggerInteraction.Ignore);

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, placementMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0)
            return false;

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit candidate in hits)
        {
            if (!candidate.collider.CompareTag("PinpointMarker"))
            {
                hit = candidate;
                return true;
            }
        }

        return false;
    }

    private void UpdatePlacementPreview()
    {
        if (!showPlacementPreview ||
            interactionMode != PinpointInteractionMode.PlaceMarker ||
            _interactionInputProvider == null ||
            _interactionInputProvider.IsTextInputActive() ||
            _interactionInputProvider.IsSceneActionBlockedByUi() ||
            !TryRaycastScene(out RaycastHit hit, true))
        {
            SetPlacementPreviewVisible(false);
            return;
        }

        EnsurePlacementPreview();

        _placementPreview.transform.position = hit.point + hit.normal * placementPreviewSurfaceOffset;
        _placementPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        _placementPreview.transform.localScale = Vector3.one * placementPreviewSize;
        SetPlacementPreviewVisible(true);
    }

    private void EnsurePlacementPreview()
    {
        if (_placementPreview != null)
            return;

        _placementPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _placementPreview.name = "PlacementPreview";
        _placementPreview.tag = "Untagged";
        _placementPreview.transform.SetParent(transform, true);

        var collider = _placementPreview.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        _placementPreviewRenderer = _placementPreview.GetComponent<Renderer>();
        _placementPreviewMaterial = new Material(FindPreviewShader());
        _placementPreviewMaterial.color = placementPreviewColor;
        _placementPreviewMaterial.SetFloat("_Surface", 1f);
        _placementPreviewMaterial.SetFloat("_Blend", 0f);
        _placementPreviewMaterial.renderQueue = 3000;

        if (_placementPreviewRenderer != null)
            _placementPreviewRenderer.material = _placementPreviewMaterial;

        SetPlacementPreviewVisible(false);
    }

    private Shader FindPreviewShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("Unlit/Color");
        if (shader != null)
            return shader;

        return Shader.Find("Standard");
    }

    private void SetPlacementPreviewVisible(bool visible)
    {
        if (_placementPreview != null && _placementPreview.activeSelf != visible)
            _placementPreview.SetActive(visible);
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

    private PinpointAnalysisExportDto CreateAnalysisExportDtoFromScene()
    {
        var analysisExport = new PinpointAnalysisExportDto
        {
            exportedAtUtc = PinpointTimestamp.NowUtcIso(),
            sessionName = "Pinpoint Session",
            sessionLastSavedAtUtc = _lastSavedAtUtc,
            sessionHasUnsavedChanges = _isDirty
        };

        foreach (var marker in _markers)
        {
            if (marker == null) continue;

            var data = marker.GetComponent<PinpointMarkerModel>();
            if (data == null) continue;

            var anchor = CreateAnchorDto(marker.transform);
            analysisExport.observations.Add(new PinpointAnalysisObservationDto
            {
                markerId = data.MarkerId,
                title = data.Title,
                severityValue = (int)data.Severity,
                severityLabel = data.Severity.ToString(),
                statusValue = (int)data.Status,
                statusLabel = data.Status.ToString(),
                rawNote = data.RawNote,
                normalizedNote = "",
                createdAtUtc = data.CreatedAtUtc,
                updatedAtUtc = data.UpdatedAtUtc,
                anchor = anchor,
                anchorSourceLabel = anchor.Source.ToString(),
                position = anchor.position,
                analysisContext = BuildAnalysisContext(data, anchor)
            });
        }

        analysisExport.markerCount = analysisExport.observations.Count;
        return analysisExport;
    }

    private string BuildAnalysisContext(PinpointMarkerModel marker, MarkerAnchorDto anchor)
    {
        return
            $"Marker '{marker.Title}' is {marker.Status} with {marker.Severity} severity. " +
            $"Anchor source is {anchor.Source}. " +
            $"Raw note: {marker.RawNote}";
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

    public void ExportAnalysisJson()
    {
        var analysisExport = CreateAnalysisExportDtoFromScene();
        PinpointAnalysisExportStorage.Save(analysisExport);
        Debug.Log($"Analysis export contains {analysisExport.markerCount} marker observations.");
    }

    public void LoadSession()
    {
        var session = PinpointSessionStorage.Load();
        LoadSessionFromDto(session);
        SetInteractionMode(PinpointInteractionMode.Select);
        MarkClean();
        RefreshSessionStatusText();
        Debug.Log("Session Loaded");
    }

    public void NewSession()
    {
        ClearAllMarkers();
        _lastSavedAtUtc = "";
        SetInteractionMode(PinpointInteractionMode.Select);
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

        string modeLabel = interactionMode == PinpointInteractionMode.PlaceMarker ? "Place" : "Select";
        sessionStatusText.text =
            $"Mode: {modeLabel} | Markers: {_markers.Count} | Saved: {PinpointTimestamp.FormatDisplay(_lastSavedAtUtc)}";
    }
}
