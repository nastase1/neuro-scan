namespace NeuroScan.Domain.Entities;

public class Patient : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string MedicalRecordNumber { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public ICollection<MriScan> MriScans { get; set; } = new List<MriScan>();
}
