namespace NeuroScan.Domain.Entities;

public class User : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required UserRole Role { get; set; }

    // Navigation properties
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    public ICollection<MriScan> ReviewedScans { get; set; } = new List<MriScan>();
}

public enum UserRole
{
    StandardUser = 0,
    Doctor = 1
}
