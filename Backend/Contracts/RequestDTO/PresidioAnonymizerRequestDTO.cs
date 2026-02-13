using System.Text.Json.Serialization;
public class PresidioAnonymizerRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("analyzer_results")]
    public List<PresidioAnalyzerResponseDTO> AnalyzerResults { get; set; } = new();

    [JsonPropertyName("operators")]
    public Dictionary<string, PresidioOperatorDTO> Operators { get; set; } = new();
}
