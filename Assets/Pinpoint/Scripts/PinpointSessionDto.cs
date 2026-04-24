using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class PinpointSessionDto
{
    public string sessionName;
    public string lastSavedAtUtc;
    public List<PinpointMarkerDto> markers = new();
}

[Serializable]
public class PinpointMarkerDto
{
    public string markerId;
    public string createdAtUtc;
    public string updatedAtUtc;
    public string title;
    public int severity;
    public int status;
    public string rawNote;
    public MarkerAnchorDto anchor;
    public Vector3 position;
}

[Serializable]
public class PinpointAnalysisExportDto
{
    public string schemaVersion = "pinpoint.analysis.v1";
    public string exportedAtUtc;
    public string sessionName;
    public string sessionLastSavedAtUtc;
    public bool sessionHasUnsavedChanges;
    public int markerCount;
    public List<PinpointAnalysisObservationDto> observations = new();
}

[Serializable]
public class PinpointAnalysisObservationDto
{
    public string markerId;
    public string title;
    public int severityValue;
    public string severityLabel;
    public int statusValue;
    public string statusLabel;
    public string rawNote;
    public string normalizedNote;
    public string createdAtUtc;
    public string updatedAtUtc;
    public MarkerAnchorDto anchor;
    public string anchorSourceLabel;
    public Vector3 position;
    public string analysisContext;
}

public static class PinpointTimestamp
{
    private const string DisplayFormat = "yyyy-MM-dd HH:mm 'UTC'";

    public static string NowUtcIso()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    public static string EnsureUtcIso(string value, string fallbackUtcIso = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallbackUtcIso ?? NowUtcIso();

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }

        return fallbackUtcIso ?? NowUtcIso();
    }

    public static string FormatDisplay(string utcIso, string emptyLabel = "Never")
    {
        if (string.IsNullOrWhiteSpace(utcIso))
            return emptyLabel;

        if (DateTime.TryParse(
                utcIso,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime().ToString(DisplayFormat, CultureInfo.InvariantCulture);
        }

        return emptyLabel;
    }
}
