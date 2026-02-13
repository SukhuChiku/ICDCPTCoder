namespace Backend.Contracts;

using Backend.Contracts.RequestDTO;
using Backend.Contracts.ResponseDTO;

public interface IAddDoctorsNoteService
{
    Task<AddDoctorsNoteResponseDTO> AddOrUpdateNoteAsync(
        AddDoctorsNoteRequestDTO request);
}
