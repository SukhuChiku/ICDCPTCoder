using Backend.Models;

public interface IAddDoctorsNoteRespository
{
    Task<PatientVisits?> GetAsync(int patientId, int visitId);
    Task AddAsync(PatientVisits visit);
    Task UpdateAsync(PatientVisits visit);
}
