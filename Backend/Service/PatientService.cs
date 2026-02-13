using Backend.Contracts;
using Backend.Contracts.ResponseDTO;
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Service;
public class PatientService : IPatientService
{
    private readonly AppDbContext _db;
    private readonly IPhiRedactionService _phiRedactionService;
    private readonly IPresidioService _presidioService; 

    public PatientService(AppDbContext db, IPhiRedactionService phiRedactionService, IPresidioService presidioService)
    {
        _db = db;
        _phiRedactionService = phiRedactionService;
        _presidioService = presidioService;

    }

    public async Task<List<PatientVisitsResponseDTO>> GetPatientVisits(int patientId)
    {
        var visits = await _db.PatientVisits
            .Where(v => v.PatientID == patientId)
            .OrderBy(v => v.VisitDate)
            .ToListAsync();

        var results = new List<PatientVisitsResponseDTO>();
        foreach (var v in visits)
        {
            try
            {
                var redactedText = await _presidioService.AnalyzeAndAnonymizeAsync(v.DoctorsNote);
                results.Add(new PatientVisitsResponseDTO
                {
                    VisitID = v.VisitID,
                    DoctorsNote = redactedText,
                    VisitDate = v.VisitDate,
                    UpdatedAt = v.UpdatedAt,
                    PhiRedacted = true
                });
            }
            catch (Exception)
            {
                // If Presidio service fails, return error message
                results.Add(new PatientVisitsResponseDTO
                {
                    VisitID = v.VisitID,
                    DoctorsNote = "Could not redact doctors note",
                    VisitDate = v.VisitDate,
                    UpdatedAt = v.UpdatedAt,
                    PhiRedacted = false
                });
            }
        }
        return results;
    }
}
