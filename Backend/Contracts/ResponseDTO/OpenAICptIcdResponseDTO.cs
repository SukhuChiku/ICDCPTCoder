public class OpenAICptIcdResponseDTO
{
    public List<CptCodeItem> CptCodes { get; set; } = new();
    public List<IcdCodeItem> IcdCodes { get; set; } = new();
}

public class CptCodeItem
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class IcdCodeItem
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}