using Microsoft.EntityFrameworkCore;
using NeuroScan.Application.IServices;
using NeuroScan.Application.DTOs;
using NeuroScan.Domain.Entities;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;

    public AdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminStatsDTO> GetStatsAsync()
    {
        var stats = new AdminStatsDTO
        {
            TotalUsers = await _db.Users.CountAsync(u => u.DeletedAt == null && u.Role == UserRole.StandardUser),
            TotalDoctors = await _db.Users.CountAsync(u => u.DeletedAt == null && u.Role == UserRole.Doctor),
            TotalPatients = await _db.Patients.CountAsync(p => p.DeletedAt == null),
            TotalScans = await _db.MriScans.CountAsync(s => s.DeletedAt == null),
            PendingReviews = await _db.MriScans.CountAsync(s => s.DeletedAt == null && s.Status == ScanStatus.Analyzed && s.ReviewedByDoctorId == null),
            AnalyzedScans = await _db.MriScans.CountAsync(s => s.DeletedAt == null && s.Status == ScanStatus.Analyzed),
            ReviewedScans = await _db.MriScans.CountAsync(s => s.DeletedAt == null && s.Status == ScanStatus.ReviewedByDoctor),
            FailedScans = await _db.MriScans.CountAsync(s => s.DeletedAt == null && s.Status == ScanStatus.Failed),
        };
        return stats;
    }

    public async Task<PagedResult<AdminUserDTO>> GetUsersAsync(string? search, int? role, int page, int pageSize)
    {
        var query = _db.Users
            .Where(u => u.DeletedAt == null)
            .AsQueryable();

        if (role.HasValue)
            query = query.Where(u => (int)u.Role == role.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var doctorIds = users.Where(u => u.Role == UserRole.Doctor).Select(u => u.Id).ToList();

        var patientCounts = await _db.Patients
            .Where(p => p.DeletedAt == null && doctorIds.Contains(p.CreatedByUserId))
            .GroupBy(p => p.CreatedByUserId)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToListAsync();

        var scanCounts = await _db.MriScans
            .Where(s => s.DeletedAt == null && s.ReviewedByDoctorId.HasValue && doctorIds.Contains(s.ReviewedByDoctorId.Value))
            .GroupBy(s => s.ReviewedByDoctorId)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToListAsync();

        var assignedDoctorIds = users
            .Where(u => u.AssignedDoctorId.HasValue)
            .Select(u => u.AssignedDoctorId!.Value)
            .Distinct()
            .ToList();
        var doctorNames = await _db.Users
            .Where(u => assignedDoctorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var items = users.Select(u => new AdminUserDTO
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            InviteCode = u.InviteCode,
            AssignedDoctorId = u.AssignedDoctorId,
            AssignedDoctorName = u.AssignedDoctorId.HasValue && doctorNames.TryGetValue(u.AssignedDoctorId.Value, out var dn) ? dn : null,
            PatientCount = patientCounts.FirstOrDefault(pc => pc.DoctorId == u.Id)?.Count ?? 0,
            ScanCount = scanCounts.FirstOrDefault(sc => sc.DoctorId == u.Id)?.Count ?? 0,
        }).ToList();

        return new PagedResult<AdminUserDTO>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDTO?> GetUserByIdAsync(Guid id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (u == null) return null;

        string? assignedDoctorName = null;
        if (u.AssignedDoctorId.HasValue)
        {
            var doc = await _db.Users.FirstOrDefaultAsync(d => d.Id == u.AssignedDoctorId.Value);
            if (doc != null) assignedDoctorName = $"{doc.FirstName} {doc.LastName}";
        }

        var patientCount = u.Role == UserRole.Doctor
            ? await _db.Patients.CountAsync(p => p.DeletedAt == null && p.CreatedByUserId == u.Id)
            : 0;
        var scanCount = u.Role == UserRole.Doctor
            ? await _db.MriScans.CountAsync(s => s.DeletedAt == null && s.ReviewedByDoctorId == u.Id)
            : 0;

        return new AdminUserDTO
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            InviteCode = u.InviteCode,
            AssignedDoctorId = u.AssignedDoctorId,
            AssignedDoctorName = assignedDoctorName,
            PatientCount = patientCount,
            ScanCount = scanCount,
        };
    }

    public async Task<bool> UpdateUserAsync(Guid id, AdminUpdateUserDTO dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user == null) return false;

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var conflict = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id && u.DeletedAt == null);
            if (conflict) return false;
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;

        if (dto.Role == UserRole.Doctor && user.Role != UserRole.Doctor && user.InviteCode == null)
        {
            string code;
            do { code = "DR-" + Guid.NewGuid().ToString("N")[..8].ToUpper(); }
            while (await _db.Users.AnyAsync(u => u.InviteCode == code));
            user.InviteCode = code;
        }

        user.Role = dto.Role;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user == null) return false;
        if (user.Role == UserRole.Admin) return false;

        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetUserPasswordAsync(Guid id, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AdminDoctorDTO>> GetDoctorsAsync()
    {
        var doctors = await _db.Users
            .Where(u => u.DeletedAt == null && u.Role == UserRole.Doctor)
            .OrderBy(u => u.LastName)
            .ToListAsync();

        var doctorIds = doctors.Select(d => d.Id).ToList();

        var patients = await _db.Patients
            .Where(p => p.DeletedAt == null && doctorIds.Contains(p.CreatedByUserId))
            .ToListAsync();

        var patientIds = patients.Select(p => p.Id).ToList();

        var scansByPatient = await _db.MriScans
            .Where(s => s.DeletedAt == null && patientIds.Contains(s.PatientId))
            .GroupBy(s => s.PatientId)
            .Select(g => new { PatientId = g.Key, Count = g.Count() })
            .ToListAsync();

        var reviewsByDoctor = await _db.MriScans
            .Where(s => s.DeletedAt == null && s.ReviewedByDoctorId.HasValue && doctorIds.Contains(s.ReviewedByDoctorId.Value))
            .GroupBy(s => s.ReviewedByDoctorId!.Value)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToListAsync();

        return doctors.Select(d =>
        {
            var docPatients = patients.Where(p => p.CreatedByUserId == d.Id).ToList();
            return new AdminDoctorDTO
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Email = d.Email,
                InviteCode = d.InviteCode,
                CreatedAt = d.CreatedAt,
                PatientCount = docPatients.Count,
                ScanCount = docPatients.Sum(p => scansByPatient.FirstOrDefault(s => s.PatientId == p.Id)?.Count ?? 0),
                ReviewCount = reviewsByDoctor.FirstOrDefault(r => r.DoctorId == d.Id)?.Count ?? 0,
                Patients = docPatients.Select(p => new AdminPatientSummaryDTO
                {
                    Id = p.Id,
                    FullName = $"{p.FirstName} {p.LastName}",
                    MedicalRecordNumber = p.MedicalRecordNumber,
                    ScanCount = scansByPatient.FirstOrDefault(s => s.PatientId == p.Id)?.Count ?? 0,
                    CreatedAt = p.CreatedAt
                }).ToList()
            };
        }).ToList();
    }

    public async Task<PagedResult<AdminScanDTO>> GetScansAsync(string? search, int? status, int page, int pageSize)
    {
        var query = _db.MriScans
            .Where(s => s.DeletedAt == null)
            .Include(s => s.Patient)
            .Include(s => s.ReviewedByDoctor)
            .Include(s => s.AnalysisResult)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(s => (int)s.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(sc =>
                sc.OriginalFileName.ToLower().Contains(s) ||
                sc.Patient.FirstName.ToLower().Contains(s) ||
                sc.Patient.LastName.ToLower().Contains(s) ||
                sc.Patient.MedicalRecordNumber.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var scans = await query
            .OrderByDescending(s => s.UploadDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = scans.Select(s => new AdminScanDTO
        {
            Id = s.Id,
            OriginalFileName = s.OriginalFileName,
            UploadDate = s.UploadDate,
            Status = s.Status,
            PatientId = s.PatientId,
            PatientName = s.Patient != null ? $"{s.Patient.FirstName} {s.Patient.LastName}" : null,
            PatientMrn = s.Patient?.MedicalRecordNumber,
            ReviewedByDoctorId = s.ReviewedByDoctorId,
            ReviewedByDoctorName = s.ReviewedByDoctor != null ? $"{s.ReviewedByDoctor.FirstName} {s.ReviewedByDoctor.LastName}" : null,
            ReviewedAt = s.ReviewedAt,
            DoctorApproved = s.AnalysisResult?.DoctorApproved,
            EpilepsyRiskLevel = s.AnalysisResult?.EpilepsyRiskLevel,
        }).ToList();

        return new PagedResult<AdminScanDTO>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> DeleteScanAsync(Guid id)
    {
        var scan = await _db.MriScans.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
        if (scan == null) return false;

        scan.DeletedAt = DateTime.UtcNow;
        scan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
