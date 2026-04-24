using System.IO;
using UnityEngine;

public class PinpointSessionStorage
{
    public static string DefaultPath =>
        Path.Combine(Application.persistentDataPath, "pinpoint_session.json");

    public static void Save(PinpointSessionDto session, string path = null)
    {
        string fullPath = path ?? DefaultPath;
        string json = JsonUtility.ToJson(session, true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Saved session to: {path ?? DefaultPath}");
    }

    public static PinpointSessionDto Load(string path = null)
    {
        string fullPath = path ?? DefaultPath;
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"No session file found at: {fullPath}");
            return null;
        }

        string json = File.ReadAllText(fullPath);
        return JsonUtility.FromJson<PinpointSessionDto>(json);
    }
}

public static class PinpointAnalysisExportStorage
{
    public static string DefaultPath =>
        Path.Combine(Application.persistentDataPath, "pinpoint_analysis_export.json");

    public static void Save(PinpointAnalysisExportDto analysisExport, string path = null)
    {
        string fullPath = path ?? DefaultPath;
        string json = JsonUtility.ToJson(analysisExport, true);
        File.WriteAllText(fullPath, json);
        Debug.Log($"Exported analysis package to: {fullPath}");
    }
}
