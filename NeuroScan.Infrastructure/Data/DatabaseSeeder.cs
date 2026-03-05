using Microsoft.EntityFrameworkCore;
using NeuroScan.Domain.Entities;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure existing doctors have invite codes (handles post-migration updates)
        var existingDoctors = await context.Users
            .Where(u => u.Role == UserRole.Doctor && u.InviteCode == null)
            .ToListAsync();
        foreach (var d in existingDoctors)
        {
            // Generate a unique code per doctor, retrying if there's a collision
            string code;
            do { code = GenerateInviteCode(); }
            while (await context.Users.AnyAsync(u => u.InviteCode == code));
            d.InviteCode = code;
        }
        if (existingDoctors.Count > 0)
            await context.SaveChangesAsync();

        if (await context.Users.AnyAsync())
        {
            return; // Database already seeded
        }

        // Create test users
        var doctor = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dr. John",
            LastName = "Smith",
            Email = "doctor@neuroscan.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("doctor123"),
            Role = UserRole.Doctor,
            InviteCode = GenerateInviteCode(),
            CreatedAt = DateTime.UtcNow
        };

        var standardUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "user@neuroscan.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
            Role = UserRole.StandardUser,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(doctor, standardUser);

        // Create test patients
        var patients = new List<Patient>
        {
            new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = "Alice",
                LastName = "Johnson",
                DateOfBirth = new DateTime(1985, 5, 15),
                MedicalRecordNumber = "MRN-001",
                CreatedByUserId = standardUser.Id,
                CreatedAt = DateTime.UtcNow
            },
            new Patient
            {
                Id = Guid.NewGuid(),
                FirstName = "Bob",
                LastName = "Williams",
                DateOfBirth = new DateTime(1972, 8, 22),
                MedicalRecordNumber = "MRN-002",
                CreatedByUserId = standardUser.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Patients.AddRangeAsync(patients);
        await context.SaveChangesAsync();

        Console.WriteLine("Database seeded successfully!");
        Console.WriteLine($"Doctor login: doctor@neuroscan.com / doctor123");
        Console.WriteLine($"User login: user@neuroscan.com / user123");
    }

    private static string GenerateInviteCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"DR-{code}";
    }
}
