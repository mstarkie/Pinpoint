using System.IO;
using UnityEngine;

public static class PinpointAiAnalysisStorage
{
    public static string LatestPath =>
        Path.Combine(Application.persistentDataPath, "pinpoint_ai_analysis_latest.json");

    public static string LogPath =>
        Path.Combine(Application.persistentDataPath, "pinpoint_ai_analysis_log.jsonl");

    public static void Save(PinpointAiAnalysisResultDto result, string latestPath = null, string logPath = null)
    {
        string fullLatestPath = latestPath ?? LatestPath;
        string fullLogPath = logPath ?? LogPath;
        string latestJson = JsonUtility.ToJson(result, true);
        string logJson = JsonUtility.ToJson(result, false);

        EnsureDirectory(fullLatestPath);
        EnsureDirectory(fullLogPath);

        File.WriteAllText(fullLatestPath, latestJson);
        File.AppendAllText(fullLogPath, logJson + "\n");

        Debug.Log($"Saved AI analysis result to: {fullLatestPath}");
        Debug.Log($"Appended AI analysis result to: {fullLogPath}");
    }

    private static void EnsureDirectory(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
