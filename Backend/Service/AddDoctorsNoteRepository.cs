using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

public class AddDoctorsNoteRepository : IAddDoctorsNoteRespository
{
    private readonly AppDbContext _context;

    public AddDoctorsNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<PatientVisits?> GetAsync(int patientId, int visitId)
        => _context.PatientVisits.FirstOrDefaultAsync(v =>
            v.PatientID == patientId &&
            v.VisitID == visitId);

    public async Task AddAsync(PatientVisits visit)
    {
        _context.PatientVisits.Add(visit);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PatientVisits visit)
    {
        visit.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
