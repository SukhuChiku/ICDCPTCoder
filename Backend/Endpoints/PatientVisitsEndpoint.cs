using Backend.Contracts;

namespace Backend.Endpoints;

public static class PatientVisitsEndpoint
{
    public static void MapPatientVisitsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/PatientVisits", async (
            int patientId,
            IPatientService service
        ) =>
        {
            try
            {
                var visits = await service.GetPatientVisits(patientId);
                return Results.Ok(visits);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

    }
}


