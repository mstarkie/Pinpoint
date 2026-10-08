using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

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
    [SerializeField] private TMP_Text saveButtonLabel;
    [SerializeField] private TMP_Text sessionStatusText;

    [Header("Interaction")]
    [SerializeField] private PinpointInteractionMode interactionMode = PinpointInteractionMode.Select;
    [SerializeField] private bool returnToSelectAfterPlacement = true;
    [SerializeField] private Button selectModeButton;
    [SerializeField] private Button placeModeButton;

    [Header("AI Analysis")]
    [SerializeField] private TMP_Text aiFeedbackText;
    [SerializeField] private Button analyzeAiButton;
    [SerializeField] private bool createAiFeedbackPanelIfMissing = true;
    [SerializeField] private string llmProviderName = "Ollama";
    [SerializeField] private string llmEndpoint = "http://localhost:11434/api/generate";
    [SerializeField] private string llmModel = "llama3.2:3b";
    [SerializeField] private int llmTimeoutSeconds = 60;
    [SerializeField] [TextArea(3, 8)] private string aiTaskInstructions =
        "Analyze the marker observations for issue severity, likely field risks, and recommended follow-up actions.";

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
    private bool _isAnalyzingWithAi;
    private string _lastSavedAtUtc;
    private const string SaveCleanLabel = "Save";
    private const string SaveDirtyLabel = "Save*";
    private const string AiReadyMessage = "AI Analysis: Ready";

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

        EnsureAiFeedbackDisplay();
        if (analyzeAiButton != null)
            analyzeAiButton.onClick.AddListener(AnalyzeSessionWithAi);
        SetAiFeedbackMessage(AiReadyMessage);

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

        if (analyzeAiButton != null)
            analyzeAiButton.onClick.RemoveListener(AnalyzeSessionWithAi);
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

        if (_interactionInputProvider.WasAnalyzeWithAiRequested())
            AnalyzeSessionWithAi();
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

    public void AnalyzeSessionWithAi()
    {
        if (_isAnalyzingWithAi)
        {
            SetAiFeedbackMessage("AI Analysis: request already running.");
            return;
        }

        var analysisExport = CreateAnalysisExportDtoFromScene();
        if (analysisExport.markerCount == 0)
        {
            SetAiFeedbackMessage("AI Analysis: add at least one marker before running analysis.");
            return;
        }

        var promptPackage = PinpointAiPromptBuilder.BuildPromptPackage(analysisExport, aiTaskInstructions);
        StartCoroutine(RequestAiAnalysisRoutine(promptPackage));
    }

    private IEnumerator RequestAiAnalysisRoutine(PinpointAiPromptPackageDto promptPackage)
    {
        _isAnalyzingWithAi = true;

        string promptJson = PinpointAiPromptBuilder.ToPromptJson(promptPackage, true);
        string promptText = PinpointAiPromptBuilder.BuildLlmPrompt(promptPackage);
        var result = new PinpointAiAnalysisResultDto
        {
            runId = Guid.NewGuid().ToString("N"),
            requestedAtUtc = PinpointTimestamp.NowUtcIso(),
            providerName = llmProviderName,
            endpoint = llmEndpoint,
            model = llmModel,
            markerCount = promptPackage.markerCount,
            promptSchemaVersion = promptPackage.schemaVersion,
            promptJson = promptJson,
            promptText = promptText
        };

        SetAiFeedbackMessage($"AI Analysis: sending {promptPackage.markerCount} marker observations to {llmProviderName}...");

        if (string.IsNullOrWhiteSpace(llmEndpoint) || string.IsNullOrWhiteSpace(llmModel))
        {
            result.success = false;
            result.errorMessage = "LLM endpoint or model is not configured.";
            CompleteAiAnalysisResult(result);
            yield break;
        }

        var requestDto = new PinpointOllamaGenerateRequestDto
        {
            model = llmModel,
            prompt = promptText,
            stream = false
        };

        string requestJson = JsonUtility.ToJson(requestDto);
        byte[] requestBody = Encoding.UTF8.GetBytes(requestJson);

        using (var request = new UnityWebRequest(llmEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(requestBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.Max(1, llmTimeoutSeconds);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string responseBody = request.downloadHandler != null ? request.downloadHandler.text : "";

            if (request.result != UnityWebRequest.Result.Success)
            {
                result.success = false;
                result.rawResponse = responseBody;
                result.errorMessage = string.IsNullOrWhiteSpace(responseBody)
                    ? $"{request.responseCode} {request.error}"
                    : $"{request.responseCode} {request.error}: {responseBody}";
            }
            else if (!TryExtractOllamaResponse(responseBody, out string responseText, out string responseError))
            {
                result.success = false;
                result.rawResponse = responseBody;
                result.errorMessage = responseError;
            }
            else
            {
                result.success = true;
                result.rawResponse = responseText;
                result.displayResponse = PinpointAiPromptBuilder.BuildDisplayResponse(responseText);
            }
        }

        CompleteAiAnalysisResult(result);
    }

    private void CompleteAiAnalysisResult(PinpointAiAnalysisResultDto result)
    {
        result.completedAtUtc = PinpointTimestamp.NowUtcIso();

        if (!result.success)
        {
            result.displayResponse =
                "AI Analysis failed.\n" +
                result.errorMessage + "\n\n" +
                "The structured prompt was still saved in the offline AI log.";
        }

        PinpointAiAnalysisStorage.Save(result);
        SetAiFeedbackMessage(result.displayResponse);
        Debug.Log(result.success
            ? $"AI analysis completed for {result.markerCount} marker observations."
            : $"AI analysis failed: {result.errorMessage}");

        _isAnalyzingWithAi = false;
    }

    private static bool TryExtractOllamaResponse(string responseBody, out string responseText, out string errorMessage)
    {
        responseText = responseBody;
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            responseText = "";
            return true;
        }

        try
        {
            var responseDto = JsonUtility.FromJson<PinpointOllamaGenerateResponseDto>(responseBody);
            if (responseDto != null)
            {
                if (!string.IsNullOrWhiteSpace(responseDto.error))
                {
                    errorMessage = responseDto.error;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(responseDto.response))
                    responseText = responseDto.response;
            }
        }
        catch (Exception parseException)
        {
            Debug.LogWarning($"AI response was not parsed as Ollama JSON; using raw response text. {parseException.Message}");
        }

        return true;
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
        SetAiFeedbackMessage(AiReadyMessage);
        Debug.Log("Markers Cleared");
    }

    private void EnsureAiFeedbackDisplay()
    {
        if (aiFeedbackText != null || !createAiFeedbackPanelIfMissing)
            return;

        Canvas canvas = sessionStatusText != null
            ? sessionStatusText.GetComponentInParent<Canvas>()
            : null;

        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("PinpointSimulatorController: no Canvas found for AI feedback display.");
            return;
        }

        var panel = new GameObject("AiFeedbackPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(20f, 20f);
        panelRect.sizeDelta = new Vector2(560f, 190f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.07f, 0.88f);

        var titleText = CreateRuntimeText("AiFeedbackTitle", panel.transform, 18f, FontStyles.Bold);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(16f, -44f);
        titleRect.offsetMax = new Vector2(-126f, -10f);
        titleText.text = "AI Analysis";

        aiFeedbackText = CreateRuntimeText("AiFeedbackText", panel.transform, 14f, FontStyles.Normal);
        var feedbackRect = aiFeedbackText.rectTransform;
        feedbackRect.anchorMin = new Vector2(0f, 0f);
        feedbackRect.anchorMax = new Vector2(1f, 1f);
        feedbackRect.offsetMin = new Vector2(16f, 16f);
        feedbackRect.offsetMax = new Vector2(-16f, -54f);

        if (analyzeAiButton == null)
            analyzeAiButton = CreateRuntimeAnalyzeButton(panel.transform);
    }

    private TMP_Text CreateRuntimeText(string name, Transform parent, float fontSize, FontStyles fontStyle)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.TopLeft;

        return text;
    }

    private Button CreateRuntimeAnalyzeButton(Transform parent)
    {
        var buttonObject = new GameObject("AnalyzeAiButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-14f, -12f);
        buttonRect.sizeDelta = new Vector2(104f, 34f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.44f, 0.95f, 0.95f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var label = CreateRuntimeText("Label", buttonObject.transform, 15f, FontStyles.Bold);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = "Analyze";

        return button;
    }

    private void SetAiFeedbackMessage(string message)
    {
        EnsureAiFeedbackDisplay();

        if (aiFeedbackText != null)
            aiFeedbackText.text = string.IsNullOrWhiteSpace(message) ? AiReadyMessage : message;
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
