namespace Backend.Contracts.RequestDTO;

public class AddDoctorsNoteRequestDTO
{
    public int PatientId { get; set; }
    public int VisitId { get; set; }
    public DateTime VisitDate { get; set; }
    public string DoctorsNote { get; set; } = string.Empty;

    // controls overwrite behavior
    public bool ForceUpdate { get; set; }
}
