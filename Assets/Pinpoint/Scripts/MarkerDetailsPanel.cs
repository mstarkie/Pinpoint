using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MarkerDetailsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text markerIdText;
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
        titleInput.SetTextWithoutNotify(_model.Title);
        severityDropdown.SetValueWithoutNotify((int)_model.Severity);
        statusDropdown.SetValueWithoutNotify((int)_model.Status);
        rawNoteInput.SetTextWithoutNotify(_model.RawNote);
    }

    
    
    public void OnTitleChanged(string value)
    {
        if (_model != null) {
            _model.Title = value;
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnSeverityChanged(int index)
    {
        if (_model != null) {
            _model.Severity = (MarkerSeverity)index;
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnStatusChanged(int index)
    {
        if (_model != null) {
            _model.Status = (MarkerStatus)index;
            OnMarkerEdited?.Invoke();
        }
    }

    private void OnRawNoteChanged(string value)
    {
        if (_model != null) {
            _model.RawNote = value;
            OnMarkerEdited?.Invoke();
        }
    }

    
}
