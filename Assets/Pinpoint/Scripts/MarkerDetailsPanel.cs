using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarkerDetailsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text markerIdText;
    [SerializeField] private TMP_Text markerTraceText;
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_Dropdown severityDropdown;
    [SerializeField] private TMP_Dropdown statusDropdown;
    [SerializeField] private TMP_InputField rawNoteInput;
    [SerializeField] private GameObject contentRoot;

    private PinpointMarkerModel _model;
    public System.Action OnMarkerEdited;
    private void Awake()
    {
        titleInput.onValueChanged.AddListener(OnTitleChanged);
        severityDropdown.onValueChanged.AddListener(OnSeverityChanged);
        statusDropdown.onValueChanged.AddListener(OnStatusChanged);
        rawNoteInput.onValueChanged.AddListener(OnRawNoteChanged);

        Bind(null);
    }

     public void Bind(PinpointMarkerModel data)
    {
        _model = data;
        bool hasSelection = _model != null;

        if (panelRoot != null)
            panelRoot.SetActive(hasSelection);
        if (contentRoot != null)
            contentRoot.SetActive(hasSelection);

        if (!hasSelection) return;

        markerIdText.text = _model.MarkerId;
        RefreshTraceText();
        titleInput.SetTextWithoutNotify(_model.Title);
        severityDropdown.SetValueWithoutNotify((int)_model.Severity);
        statusDropdown.SetValueWithoutNotify((int)_model.Status);
        rawNoteInput.SetTextWithoutNotify(_model.RawNote);
    }

    
    
    public void OnTitleChanged(string value)
    {
        if (_model != null) {
            _model.Title = value;
            RefreshTraceText();
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnSeverityChanged(int index)
    {
        if (_model != null) {
            _model.Severity = (MarkerSeverity)index;
            RefreshTraceText();
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnStatusChanged(int index)
    {
        if (_model != null) {
            _model.Status = (MarkerStatus)index;
            RefreshTraceText();
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnRawNoteChanged(string value)
    {
        if (_model != null) {
            _model.RawNote = value;
            RefreshTraceText();
            OnMarkerEdited?.Invoke();
        }
    }

    private void RefreshTraceText()
    {
        if (markerTraceText == null || _model == null)
            return;

        markerTraceText.text =
            $"Created: {PinpointTimestamp.FormatDisplay(_model.CreatedAtUtc, "Unknown")}\n" +
            $"Updated: {PinpointTimestamp.FormatDisplay(_model.UpdatedAtUtc, "Unknown")}";
    }
}
