using Backend.Contracts;
using Backend.Contracts.RequestDTO;
using Backend.Contracts.ResponseDTO;
using Backend.Service;

namespace Backend.Endpoints;

public static class AddDoctorsNoteEndpoint
{
    public static void MapAddDoctorsNoteEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/AddDoctorsNote",
            async (AddDoctorsNoteRequestDTO request,
                IAddDoctorsNoteService service
                ) =>
        {
            
            var result = await service.AddOrUpdateNoteAsync(request);

            return result.Status switch
            {
                VisitNoteStatus.Conflict =>
                    Results.Conflict(new
                    {
                        message = result.Message,
                        requireConfirmation = true
                    }),

                VisitNoteStatus.Updated =>
                    Results.Ok(new { message = result.Message }),

                _ =>
                    Results.Created("/api/AddDoctorsNote",
                        new { message = result.Message })
            };
        });
    }
}