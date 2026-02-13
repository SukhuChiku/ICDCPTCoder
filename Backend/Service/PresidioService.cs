using System.Net.Http.Json;
using System.Text.Json;

public class PresidioService : IPresidioService
{
    private readonly HttpClient _analyzerClient;
    private readonly HttpClient _anonymizerClient;
    private readonly IPhiRedactionService _regexService;

    // CHANGE 1: Inject IHttpClientFactory to get both clients
    public PresidioService(
        HttpClient analyzerClient,  // This is the default client (Analyzer)
        IHttpClientFactory httpClientFactory,
        IPhiRedactionService regexService)
    {
        _analyzerClient = analyzerClient;
        _anonymizerClient = httpClientFactory.CreateClient("PresidioAnonymizer");
        _regexService = regexService;
    }

    public async Task<string> AnalyzeAndAnonymizeAsync(string originalText)
    {
        // 1️⃣ Regex-based redaction FIRST
        var (regexRedacted, _) = _regexService.RedactPhiData(originalText);

        // 2️⃣ Send to Presidio Analyzer
        var analyzeRequest = new PresidioAnalyzerRequestDTO
        {
            Text = regexRedacted,
            Language = "en",
            ScoreThreshold = 0.6
        };

        // CHANGE 2: Use _analyzerClient (no need for full URL)
        var analyzeResponse = await _analyzerClient.PostAsJsonAsync(
            "/analyze",
            analyzeRequest
        );

        if (!analyzeResponse.IsSuccessStatusCode)
        {
            var errorContent = await analyzeResponse.Content.ReadAsStringAsync();
            throw new Exception($"Analyzer failed: {analyzeResponse.StatusCode} - {errorContent}");
        }

        var analyzerJson = await analyzeResponse.Content.ReadAsStringAsync();
        var analyzerResults = JsonSerializer.Deserialize<List<PresidioAnalyzerResponseDTO>>(analyzerJson, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (analyzerResults == null || analyzerResults.Count == 0)
            return regexRedacted;

        // 3️⃣ Prepare operators mapping for each entity type
        var operators = analyzerResults
            .Select(r => r.EntityType)
            .Distinct()
            .ToDictionary(
                entity => entity,
                entity => new PresidioOperatorDTO
                {
                    Type = "replace",
                    NewValue = $"[{entity}_REDACTED]"
                }
            );

        // 4️⃣ Send to Presidio Anonymizer
        var anonymizeRequest = new PresidioAnonymizerRequest
        {
            Text = regexRedacted,
            AnalyzerResults = analyzerResults,
            Operators = operators
        };

        // CHANGE 3: Use _anonymizerClient (no hardcoded localhost URL!)
        var anonymizeResponse = await _anonymizerClient.PostAsJsonAsync(
            "/anonymize",
            anonymizeRequest
        );

        if (!anonymizeResponse.IsSuccessStatusCode)
        {
            var errorContent = await anonymizeResponse.Content.ReadAsStringAsync();
            throw new Exception($"Anonymizer failed: {anonymizeResponse.StatusCode} - {errorContent}");
        }

        var anonymizeJson = await anonymizeResponse.Content.ReadAsStringAsync();
        var anonResult = JsonSerializer.Deserialize<JsonElement>(anonymizeJson);
        
        if (anonResult.TryGetProperty("text", out var textElement))
        {
            return textElement.GetString() ?? regexRedacted;
        }

        return regexRedacted;
    }
}