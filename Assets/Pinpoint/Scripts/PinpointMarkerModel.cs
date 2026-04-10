using System;
using UnityEngine;

public enum MarkerSeverity { Low, Medium, High, Critical }
public enum MarkerStatus { Open, InProgress, Blocked, Closed }
public class PinpointMarkerModel : MonoBehaviour
{
    [SerializeField] private string markerId;
    [SerializeField] private string title;
    [SerializeField] private MarkerSeverity severity = MarkerSeverity.Medium;
    [SerializeField] private MarkerStatus status = MarkerStatus.Open;
    [SerializeField] [TextArea] private string rawNote;

    public string MarkerId => markerId;
    public string Title { get => title; set => title = value; }
    public MarkerSeverity Severity { get => severity; set => severity = value; }
    public MarkerStatus Status { get => status; set => status = value; }
    public string RawNote { get => rawNote; set => rawNote = value; }

    public void InitializeNew()
    {
        markerId = Guid.NewGuid().ToString("N");
        title = $"Marker {markerId[..6]}";
        severity = MarkerSeverity.Medium;
        status = MarkerStatus.Open;
        rawNote = "";
    }

    public void LoadFromDto(PinpointMarkerDto dto)
    {
        markerId = dto.markerId;
        title = dto.title;
        severity = (MarkerSeverity)dto.severity;
        status = (MarkerStatus)dto.status;
        rawNote = dto.rawNote;
    }

    public static implicit operator GameObject(PinpointMarkerModel v)
    {
        throw new NotImplementedException();
    }
}
