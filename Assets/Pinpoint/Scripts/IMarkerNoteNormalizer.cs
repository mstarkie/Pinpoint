using System.Threading.Tasks;

/*
The idea is to keep the LLM integration away from UI and away from marker placement and provide test stubs 
before LLM integration.
*/
public interface IMarkerNoteNormalizer
{
    Task<string> NormalizeAsync(string rawNote);
}
