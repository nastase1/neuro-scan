using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NeuroScan.Domain.Entities;
using NeuroScan.Infrastructure.Context;

namespace NeuroScan.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        // Ensure existing doctors have invite codes (handles post-migration updates)
        var existingDoctors = await context.Users
            .Where(u => u.Role == UserRole.Doctor && u.InviteCode == null)
            .ToListAsync();
        foreach (var d in existingDoctors)
        {
            string code;
            do { code = GenerateInviteCode(); }
            while (await context.Users.AnyAsync(u => u.InviteCode == code));
            d.InviteCode = code;
        }
        if (existingDoctors.Count > 0)
            await context.SaveChangesAsync();

        // Always ensure the default admin account exists and has the Admin role.
        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.DeletedAt == null && u.Email.ToLower() == "admin@neuroscan.com");

        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@neuroscan.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        else if (adminUser.Role != UserRole.Admin)
        {
            adminUser.Role = UserRole.Admin;
            adminUser.FirstName = string.IsNullOrWhiteSpace(adminUser.FirstName) ? "Super" : adminUser.FirstName;
            adminUser.LastName = string.IsNullOrWhiteSpace(adminUser.LastName) ? "Admin" : adminUser.LastName;
            adminUser.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        // Bulk seed doctors and patients — runs every startup, fully idempotent
        await SeedBulkDataAsync(context, logger);

        if (await context.Users.AnyAsync(u => u.Role != UserRole.Admin))
        {
            return; // Test users already seeded
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

        logger.LogInformation("Database seeded successfully!");
        logger.LogInformation("Doctor login: doctor@neuroscan.com / doctor123");
        logger.LogInformation("User login: user@neuroscan.com / user123");
    }

    private static async Task SeedBulkDataAsync(ApplicationDbContext context, ILogger logger)
    {
        var doctorSeedData = new[]
        {
            ("Ion",       "Popescu",     "dr.ion.popescu@neuroscan.com"),
            ("Maria",     "Ionescu",     "dr.maria.ionescu@neuroscan.com"),
            ("Alexandru", "Dumitrescu",  "dr.alexandru.dumitrescu@neuroscan.com"),
            ("Elena",     "Popa",        "dr.elena.popa@neuroscan.com"),
            ("Gheorghe",  "Constantin",  "dr.gheorghe.constantin@neuroscan.com"),
            ("Ana",       "Muresan",     "dr.ana.muresan@neuroscan.com"),
            ("Mihai",     "Stoica",      "dr.mihai.stoica@neuroscan.com"),
            ("Ioana",     "Gheorghe",    "dr.ioana.gheorghe@neuroscan.com"),
            ("Cristian",  "Moldovan",    "dr.cristian.moldovan@neuroscan.com"),
            ("Laura",     "Stancu",      "dr.laura.stancu@neuroscan.com"),
            ("Andrei",    "Radu",        "dr.andrei.radu@neuroscan.com"),
            ("Daniela",   "Ene",         "dr.daniela.ene@neuroscan.com"),
            ("Florin",    "Matei",       "dr.florin.matei@neuroscan.com"),
            ("Simona",    "Tudor",       "dr.simona.tudor@neuroscan.com"),
            ("Bogdan",    "Marin",       "dr.bogdan.marin@neuroscan.com"),
            ("Roxana",    "Dima",        "dr.roxana.dima@neuroscan.com"),
            ("Vlad",      "Nistor",      "dr.vlad.nistor@neuroscan.com"),
            ("Raluca",    "Andrei",      "dr.raluca.andrei@neuroscan.com"),
            ("Sorin",     "Blaga",       "dr.sorin.blaga@neuroscan.com"),
            ("Camelia",   "Vasile",      "dr.camelia.vasile@neuroscan.com"),
        };

        var newDoctors = new List<User>();
        var existingEmails = (await context.Users
            .Select(u => u.Email.ToLower())
            .ToListAsync()).ToHashSet();

        foreach (var (first, last, email) in doctorSeedData)
        {
            if (existingEmails.Contains(email)) continue;

            string code;
            do { code = GenerateInviteCode(); }
            while (await context.Users.AnyAsync(u => u.InviteCode == code)
                   || newDoctors.Any(d => d.InviteCode == code));

            newDoctors.Add(new User
            {
                Id = Guid.NewGuid(),
                FirstName = first,
                LastName = last,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("doctor123"),
                Role = UserRole.Doctor,
                InviteCode = code,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newDoctors.Count > 0)
        {
            await context.Users.AddRangeAsync(newDoctors);
            await context.SaveChangesAsync();
            logger.LogInformation("[Seed] Added {Count} new doctors.", newDoctors.Count);
        }

        const int targetPatientCount = 500;

        var existingPatientCount = await context.Patients.CountAsync(p => p.DeletedAt == null);
        var toAdd = targetPatientCount - existingPatientCount;

        var allDoctors = await context.Users
            .Where(u => u.Role == UserRole.Doctor && u.DeletedAt == null)
            .ToListAsync();

        if (allDoctors.Count == 0) return;

        if (toAdd <= 0)
        {
            logger.LogInformation("[Seed] {Count} patients already exist, none added.", existingPatientCount);
        }

        if (toAdd > 0)
        {
            var existingMrns = await context.Patients
                .Where(p => p.MedicalRecordNumber.StartsWith("MRN-"))
                .Select(p => p.MedicalRecordNumber)
                .ToListAsync();

            int mrnCounter = 0;
            foreach (var mrn in existingMrns)
            {
                if (int.TryParse(mrn[4..], out int num) && num > mrnCounter)
                    mrnCounter = num;
            }
            mrnCounter++;

            var firstNames = new[]
            {
                "Andrei", "Maria", "Ion", "Elena", "Mihai", "Ana", "Alexandru", "Ioana", "Cristian", "Laura",
                "Bogdan", "Simona", "Vlad", "Daniela", "Florin", "Roxana", "Sorin", "Camelia", "Radu", "Alina",
                "Gabriel", "Teodora", "Marius", "Adriana", "Dan", "Luminita", "Victor", "Irina", "Octavian", "Sabrina",
                "Cosmin", "Diana", "Silviu", "Georgiana", "Darius", "Valentina", "Mircea", "Claudia", "Liviu", "Natalia",
                "Paul", "Andreea", "Ciprian", "Oana", "Catalin", "Stefania", "Razvan", "Bianca", "Dragos", "Liana"
            };

            var lastNames = new[]
            {
                "Popescu", "Ionescu", "Popa", "Constantin", "Stoica", "Dumitrescu", "Radu", "Ene",
                "Matei", "Tudor", "Marin", "Dima", "Nistor", "Blaga", "Vasile", "Cojocaru", "Lungu", "Negru",
                "Macovei", "Oprea", "Vlad", "Barbu", "Neagu", "Moldovan", "Rusu", "Miron", "Cretu", "Luca",
                "Dinu", "Avram", "Olaru", "Sandu", "Niculescu", "Toma", "Badea", "Zamfir", "Costache",
                "Serban", "Ciobanu", "Petrescu", "Manolescu", "Florea", "Grigorescu", "Alexandrescu", "Marinescu"
            };

            var random = new Random(42);
            var newPatients = new List<Patient>(toAdd);

            for (int i = 0; i < toAdd; i++)
            {
                var doctor = allDoctors[i % allDoctors.Count];
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var year = 1940 + random.Next(66);
                var month = random.Next(1, 13);
                var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
                var dob = new DateTime(year, month, day);
                var mrn = $"MRN-{mrnCounter + i:D4}";
                var daysAgo = random.Next(0, 730);

                newPatients.Add(new Patient
                {
                    Id = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = dob,
                    MedicalRecordNumber = mrn,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}{random.Next(10, 99)}@mail.com",
                    CreatedByUserId = doctor.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
                });
            }

            const int batchSize = 100;
            for (int i = 0; i < newPatients.Count; i += batchSize)
            {
                await context.Patients.AddRangeAsync(newPatients.Skip(i).Take(batchSize));
                await context.SaveChangesAsync();
            }

            logger.LogInformation("[Seed] Added {PatientCount} patients distributed across {DoctorCount} doctors.", newPatients.Count, allDoctors.Count);
        }

        const int targetStandardUserCount = 200;

        var existingStandardUserCount = await context.Users
            .CountAsync(u => u.Role == UserRole.StandardUser && u.DeletedAt == null);

        var usersToCreate = targetStandardUserCount - existingStandardUserCount;
        if (usersToCreate <= 0)
        {
            logger.LogInformation("[Seed] {Count} users already exist, none added.", existingStandardUserCount);
            return;
        }

        var existingUserEmails = (await context.Users
            .Select(u => u.Email.ToLower())
            .ToListAsync()).ToHashSet();

        var patientsForUsers = await context.Patients
            .Where(p => p.DeletedAt == null && p.UserId == null && p.Email != null)
            .OrderBy(p => p.CreatedAt)
            .Take(usersToCreate * 2) // take extra as a buffer for duplicate emails
            .ToListAsync();

        var newUsers = new List<User>();
        foreach (var patient in patientsForUsers)
        {
            if (newUsers.Count >= usersToCreate) break;
            if (patient.Email == null) continue;
            if (existingUserEmails.Contains(patient.Email.ToLower())) continue;

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                Role = UserRole.StandardUser,
                AssignedDoctorId = patient.CreatedByUserId,
                CreatedAt = patient.CreatedAt
            };

            patient.UserId = user.Id;
            newUsers.Add(user);
            existingUserEmails.Add(patient.Email.ToLower());
        }

        if (newUsers.Count > 0)
        {
            await context.Users.AddRangeAsync(newUsers);
            await context.SaveChangesAsync();
            logger.LogInformation("[Seed] Added {Count} users (StandardUser) linked to patients.", newUsers.Count);
        }
    }

    private static string GenerateInviteCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        var code = new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"DR-{code}";
    }
}
