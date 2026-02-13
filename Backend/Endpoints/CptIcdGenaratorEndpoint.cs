using Backend.Contracts.Interfaces;
public static class CptIcdGeneratorEndpoint
{
    public static void MapCptIcdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/CptIcdGenerator",
            async (OpenAICptIcdRequestDTO request, IOpenAIService openAIService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                    return Results.BadRequest("Input text is required.");

                var result = await openAIService.GenerateCptIcdAsync(request.Text);
                return Results.Ok(result);
            })
        .WithName("GenerateCptIcd")
        .WithTags("AI");
    }
}
