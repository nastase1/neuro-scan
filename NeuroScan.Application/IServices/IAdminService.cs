using NeuroScan.Domain.Entities;

namespace NeuroScan.Application.IServices;

public interface IAdminService
{
    Task<AdminStatsDTO> GetStatsAsync();
    Task<PagedResult<AdminUserDTO>> GetUsersAsync(string? search, int? role, int page, int pageSize);
    Task<AdminUserDTO?> GetUserByIdAsync(Guid id);
    Task<bool> UpdateUserAsync(Guid id, AdminUpdateUserDTO dto);
    Task<bool> DeleteUserAsync(Guid id);
    Task<IEnumerable<AdminDoctorDTO>> GetDoctorsAsync();
    Task<PagedResult<AdminScanDTO>> GetScansAsync(string? search, int? status, int page, int pageSize);
    Task<bool> DeleteScanAsync(Guid id);
    Task<bool> ResetUserPasswordAsync(Guid id, string newPassword);
}

// ---- DTOs ----

public class AdminStatsDTO
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalScans { get; set; }
    public int PendingReviews { get; set; }
    public int AnalyzedScans { get; set; }
    public int ReviewedScans { get; set; }
    public int FailedScans { get; set; }
}

public class AdminUserDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? InviteCode { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? AssignedDoctorName { get; set; }
    public int PatientCount { get; set; }
    public int ScanCount { get; set; }
}

public class AdminUpdateUserDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public UserRole Role { get; set; }
}

public class AdminResetPasswordDTO
{
    public required string NewPassword { get; set; }
}

public class AdminDoctorDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? InviteCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PatientCount { get; set; }
    public int ScanCount { get; set; }
    public int ReviewCount { get; set; }
    public List<AdminPatientSummaryDTO> Patients { get; set; } = new();
}

public class AdminPatientSummaryDTO
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public int ScanCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminScanDTO
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public ScanStatus Status { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientMrn { get; set; }
    public Guid? ReviewedByDoctorId { get; set; }
    public string? ReviewedByDoctorName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool? DoctorApproved { get; set; }
    public string? EpilepsyRiskLevel { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
