using Backend.Models;
using Backend.Contracts;
using Backend.Contracts.RequestDTO;
using Backend.Contracts.ResponseDTO;
using Backend.Data;

namespace Backend.Service;

public class AddDoctorsNoteService : IAddDoctorsNoteService
{
    private readonly IAddDoctorsNoteRespository _repo;

    public AddDoctorsNoteService(IAddDoctorsNoteRespository repo)
    {
        _repo = repo;
    }

    public async Task<AddDoctorsNoteResponseDTO> AddOrUpdateNoteAsync(
        AddDoctorsNoteRequestDTO request)
    {
        var visit = await _repo.GetAsync(
            request.PatientId, request.VisitId);

        if (visit != null && !request.ForceUpdate)
        {
            return new AddDoctorsNoteResponseDTO
            {
                Status = VisitNoteStatus.Conflict,
                Message = "Doctor's note already exists for this visit."
            };
        }

        if (visit != null)
        {
            visit.DoctorsNote = request.DoctorsNote;
            visit.VisitDate = request.VisitDate;
            await _repo.UpdateAsync(visit);

            return new AddDoctorsNoteResponseDTO
            {
                Status = VisitNoteStatus.Updated,
                Message = "Doctor's note updated successfully."
            };
        }

        var newVisit = new PatientVisits
        {
            PatientID = request.PatientId,
            VisitID = request.VisitId,
            VisitDate = request.VisitDate,
            DoctorsNote = request.DoctorsNote
        };

        await _repo.AddAsync(newVisit);

        return new AddDoctorsNoteResponseDTO
        {
            Status = VisitNoteStatus.Created,
            Message = "Doctor's note added successfully."
        };
    }
}