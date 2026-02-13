namespace Backend.Contracts;

using Backend.Contracts.ResponseDTO;

public interface IPatientService
{
 Task<List<PatientVisitsResponseDTO>> GetPatientVisits(int patientId);
}
