using System.Text.Json.Serialization;

public class PresidioAnalyzerResponseDTO
{
    [JsonPropertyName("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("analysis_explanation")]
    public string AnalysisExplanation { get; set; } = string.Empty;
}
