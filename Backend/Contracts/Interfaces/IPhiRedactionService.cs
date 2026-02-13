public interface IPhiRedactionService
{
    (string RedactedText, bool WasRedacted) RedactPhiData(string text);
}