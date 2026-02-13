namespace Backend.Contracts.ResponseDTO;

public enum VisitNoteStatus
{
    Created,
    Updated,
    Conflict
}

public class AddDoctorsNoteResponseDTO
{
    public VisitNoteStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
