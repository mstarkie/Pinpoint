using System.IO;
using UnityEngine;

public class PinpointSessionStorage
{
    public static string DefaultPath =>
        Path.Combine(Application.persistentDataPath, "pinpoint_session.json");

    public static void Save(PinpointSessionDto session, string path = null)
    {
        string json = JsonUtility.ToJson(session, true);
        File.WriteAllText(path ?? DefaultPath, json);
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
