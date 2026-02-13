namespace Backend.Contracts.ResponseDTO;

public class PatientVisitsResponseDTO
{
    public int PatientID { get; set; } 
    public int VisitID { get; set; }
    public string DoctorsNote { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Indicates if Protected Health Information (PHI) was redacted from DoctorsNote.
    /// When true, sensitive data such as SSN, phone, email, or dates have been masked.
    /// </summary>
    public bool PhiRedacted { get; set; }
}
