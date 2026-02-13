public class PresidioAnalyzerRequestDTO
{
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public double? ScoreThreshold { get; set; }
}
