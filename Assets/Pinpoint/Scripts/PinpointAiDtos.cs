using System;
using System.Collections.Generic;

[Serializable]
public class PinpointAiPromptPackageDto
{
    public string schemaVersion = "pinpoint.ai.prompt.v1";
    public string generatedAtUtc;
    public string task;
    public string outputContract;
    public string sessionName;
    public string sessionLastSavedAtUtc;
    public bool sessionHasUnsavedChanges;
    public int markerCount;
    public List<PinpointAnalysisObservationDto> observations = new();
}

[Serializable]
public class PinpointAiAnalysisResultDto
{
    public string schemaVersion = "pinpoint.ai.result.v1";
    public string runId;
    public string requestedAtUtc;
    public string completedAtUtc;
    public string providerName;
    public string endpoint;
    public string model;
    public bool success;
    public int markerCount;
    public string promptSchemaVersion;
    public string promptJson;
    public string promptText;
    public string rawResponse;
    public string displayResponse;
    public string errorMessage;
}

[Serializable]
public class PinpointOllamaGenerateRequestDto
{
    public string model;
    public string prompt;
    public bool stream;
}

[Serializable]
public class PinpointOllamaGenerateResponseDto
{
    public string model;
    public string created_at;
    public string response;
    public bool done;
    public string error;
}
