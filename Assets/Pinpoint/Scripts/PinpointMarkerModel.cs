using System;
using UnityEngine;

public enum MarkerSeverity { Low, Medium, High, Critical }
public enum MarkerStatus { Open, InProgress, Blocked, Closed }
public class PinpointMarkerModel : MonoBehaviour
{
    [SerializeField] private string markerId;
    [SerializeField] private string createdAtUtc;
    [SerializeField] private string updatedAtUtc;
    [SerializeField] private string title;
    [SerializeField] private MarkerSeverity severity = MarkerSeverity.Medium;
    [SerializeField] private MarkerStatus status = MarkerStatus.Open;
    [SerializeField] [TextArea] private string rawNote;

    public string MarkerId => markerId;
    public string CreatedAtUtc => createdAtUtc;
    public string UpdatedAtUtc => updatedAtUtc;
    public string Title
    {
        get => title;
        set
        {
            if (title == value)
                return;

            title = value;
            TouchUpdatedAt();
        }
    }
    public MarkerSeverity Severity
    {
        get => severity;
        set
        {
            if (severity == value)
                return;

            severity = value;
            TouchUpdatedAt();
        }
    }
    public MarkerStatus Status
    {
        get => status;
        set
        {
            if (status == value)
                return;

            status = value;
            TouchUpdatedAt();
        }
    }
    public string RawNote
    {
        get => rawNote;
        set
        {
            if (rawNote == value)
                return;

            rawNote = value;
            TouchUpdatedAt();
        }
    }

    public void InitializeNew()
    {
        string now = PinpointTimestamp.NowUtcIso();
        markerId = Guid.NewGuid().ToString("N");
        createdAtUtc = now;
        updatedAtUtc = now;
        title = $"Marker {markerId[..6]}";
        severity = MarkerSeverity.Medium;
        status = MarkerStatus.Open;
        rawNote = "";
    }

    public void LoadFromDto(PinpointMarkerDto dto)
    {
        markerId = dto.markerId;
        string fallbackTimestamp = PinpointTimestamp.NowUtcIso();
        createdAtUtc = PinpointTimestamp.EnsureUtcIso(dto.createdAtUtc, fallbackTimestamp);
        updatedAtUtc = PinpointTimestamp.EnsureUtcIso(dto.updatedAtUtc, createdAtUtc);
        title = dto.title;
        severity = (MarkerSeverity)dto.severity;
        status = (MarkerStatus)dto.status;
        rawNote = dto.rawNote;
    }

    private void TouchUpdatedAt()
    {
        updatedAtUtc = PinpointTimestamp.NowUtcIso();
    }

    public static implicit operator GameObject(PinpointMarkerModel v)
    {
        throw new NotImplementedException();
    }
}
