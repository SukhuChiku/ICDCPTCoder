public interface IPresidioService
{
    Task<string> AnalyzeAndAnonymizeAsync(string text);
}
