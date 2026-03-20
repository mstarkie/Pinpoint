using System.Threading.Tasks;

public class StubMarkerNoteNormalizer : IMarkerNoteNormalizer
{
    public Task<string> NormalizeAsync(string rawNote)
    {
        string result =
            $"Structured Summary:\n" +
            $"- Observed issue: {rawNote}\n" +
            $"- Recommended follow-up: Review in field\n" +
            $"- Confidence: Stub\n";

        return Task.FromResult(result);
    }
}
