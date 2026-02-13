using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Contracts.Interfaces;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public OpenAIService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<OpenAICptIcdResponseDTO> GenerateCptIcdAsync(string redactedNote)
    {
        //  CHANGE: Try environment variable first (Docker), then appsettings.json (local)
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? _config["OpenAI:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("OpenAI API key not configured. Set OPENAI_API_KEY environment variable or add to appsettings.json");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var prompt = BuildPrompt(redactedNote);

        var requestBody = new
        {
            model = "gpt-4o-mini",  
            messages = new[]
            {
                new { role = "system", content = "You are a certified medical coder." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1
        };

        var response = await _http.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            )
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI error: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("Empty OpenAI response");

        return JsonSerializer.Deserialize<OpenAICptIcdResponseDTO>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        )!;
    }


    private static string BuildPrompt(string note) =>
    $@"
You are a medical coding expert.

Based ONLY on the clinical information below, generate CPT and ICD-10 codes.

Rules:
- Do NOT hallucinate diagnoses
- Use the most specific ICD-10 codes
- CPT codes should reflect outpatient E/M when applicable
- Output STRICT JSON ONLY
- No markdown, no explanations

Return format:
{{
  ""cptCodes"": [
    {{ ""code"": ""99214"", ""description"": ""Office visit, established patient"" }}
  ],
  ""icdCodes"": [
    {{ ""code"": ""E11.65"", ""description"": ""Type 2 diabetes mellitus with hyperglycemia"" }}
  ]
}}

Clinical Note:
{note}
";
}