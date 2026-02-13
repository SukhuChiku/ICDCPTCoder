namespace Backend.Models;

public class PatientVisits
{
    public int ID { get; set; }
    public int PatientID { get; set; } 
    public int VisitID { get; set; }
    public string DoctorsNote { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
