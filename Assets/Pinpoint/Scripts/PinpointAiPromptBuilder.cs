using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class PinpointAiPromptBuilder
{
    private const string DefaultTask =
        "Analyze the marker observations for field follow-up, risk prioritization, and decision support.";

    private const string DefaultOutputContract =
        "Return concise plain text with these sections: Summary, Highest Priority Markers, Risks, Recommended Next Actions, Missing Data. " +
        "Reference marker IDs when making recommendations. Do not invent site facts that are not present in the marker data.";

    public static PinpointAiPromptPackageDto BuildPromptPackage(
        PinpointAnalysisExportDto analysisExport,
        string taskInstructions)
    {
        string task = string.IsNullOrWhiteSpace(taskInstructions)
            ? DefaultTask
            : taskInstructions.Trim();

        return new PinpointAiPromptPackageDto
        {
            generatedAtUtc = PinpointTimestamp.NowUtcIso(),
            task = task,
            outputContract = DefaultOutputContract,
            sessionName = analysisExport.sessionName,
            sessionLastSavedAtUtc = analysisExport.sessionLastSavedAtUtc,
            sessionHasUnsavedChanges = analysisExport.sessionHasUnsavedChanges,
            markerCount = analysisExport.markerCount,
            observations = analysisExport.observations == null
                ? new List<PinpointAnalysisObservationDto>()
                : new List<PinpointAnalysisObservationDto>(analysisExport.observations)
        };
    }

    public static string ToPromptJson(PinpointAiPromptPackageDto promptPackage, bool prettyPrint = true)
    {
        return JsonUtility.ToJson(promptPackage, prettyPrint);
    }

    public static string BuildLlmPrompt(PinpointAiPromptPackageDto promptPackage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are the analysis assistant for Pinpoint, an AR marker capture prototype for industrial field observations.");
        builder.AppendLine("Use only the structured marker data supplied below.");
        builder.AppendLine(promptPackage.outputContract);
        builder.AppendLine();
        builder.AppendLine("Task:");
        builder.AppendLine(promptPackage.task);
        builder.AppendLine();
        builder.AppendLine("Structured marker data JSON:");
        builder.AppendLine(ToPromptJson(promptPackage, true));
        return builder.ToString();
    }

    public static string BuildDisplayResponse(string response, int maxCharacters = 1800)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "AI analysis returned no response text.";

        string trimmed = response.Trim();
        if (trimmed.Length <= maxCharacters)
            return trimmed;

        return trimmed[..maxCharacters].TrimEnd() + "\n...";
    }
}
