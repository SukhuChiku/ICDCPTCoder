namespace Backend.Contracts.Interfaces;
public interface IOpenAIService
{
    Task<OpenAICptIcdResponseDTO> GenerateCptIcdAsync(string redactedNote);
}
