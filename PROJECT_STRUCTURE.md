# NeuroScan Project Structure

## Complete File Tree

```
C:\NeuroScan\
│
├── NeuroScan.sln                          # Solution file
│
├── NeuroScan.API/                         # 🌐 Web API Layer
│   ├── Controllers/
│   │   ├── AuthController.cs              # Authentication endpoints
│   │   ├── PatientController.cs           # Patient CRUD endpoints
│   │   └── MriScanController.cs           # MRI scan upload & analysis
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs                         # Application startup & DI configuration
│   ├── appsettings.json                   # Configuration (JWT, DB, Storage)
│   ├── appsettings.Development.json
│   ├── Dockerfile                         # Container definition
│   └── NeuroScan.API.csproj
│
├── NeuroScan.Application/                 # 💼 Business Logic Layer
│   ├── IServices/
│   │   ├── IAuthService.cs                # Auth interface + DTOs
│   │   ├── IJwtTokenService.cs            # Token generation interface
│   │   ├── IPatientService.cs             # Patient service interface
│   │   ├── IMriScanService.cs             # MRI scan service interface
│   │   ├── IAiAnalysisService.cs          # Python AI integration interface
│   │   └── IOpenAiReportService.cs        # OpenAI API interface
│   ├── Services/
│   │   ├── AuthService.cs                 # Registration, login logic
│   │   ├── PatientService.cs              # Patient business logic
│   │   └── MriScanService.cs              # Scan processing orchestration
│   └── NeuroScan.Application.csproj
│
├── NeuroScan.Domain/                      # 🎯 Core Domain Layer
│   ├── Entities/
│   │   ├── BaseEntity.cs                  # Base entity (Id, timestamps, soft delete)
│   │   ├── User.cs                        # User entity + UserRole enum
│   │   ├── Patient.cs                     # Patient entity
│   │   ├── MriScan.cs                     # MRI scan entity + ScanStatus enum
│   │   └── AnalysisResult.cs              # Analysis result entity
│   ├── IRepositories/
│   │   ├── IUserRepository.cs             # User repository interface
│   │   ├── IPatientRepository.cs          # Patient repository interface
│   │   ├── IMriScanRepository.cs          # MRI scan repository interface
│   │   └── IAnalysisResultRepository.cs   # Analysis repository interface
│   └── NeuroScan.Domain.csproj
│
├── NeuroScan.Infrastructure/              # 🔧 Infrastructure Layer
│   ├── Context/
│   │   └── ApplicationDbContext.cs        # EF Core DbContext with configurations
│   ├── Repositories/
│   │   ├── UserRepository.cs              # User data access implementation
│   │   ├── PatientRepository.cs           # Patient data access implementation
│   │   ├── MriScanRepository.cs           # MRI scan data access implementation
│   │   └── AnalysisResultRepository.cs    # Analysis data access implementation
│   ├── Services/
│   │   ├── JwtTokenService.cs             # JWT token generation
│   │   ├── AiAnalysisService.cs           # Python AI HTTP client
│   │   └── OpenAiReportService.cs         # OpenAI GPT-4 integration
│   ├── Data/
│   │   └── DatabaseSeeder.cs              # Test data seeding
│   └── NeuroScan.Infrastructure.csproj
│
├── NeuroScan.Shared/                      # 📦 Shared Types Layer
│   └── NeuroScan.Shared.csproj            # (Reserved for shared DTOs/Enums)
│
├── python-ai-service/                     # 🐍 Python AI Microservice
│   └── main.py                            # FastAPI app for MRI analysis
│
├── neuro-scan/                            # Original folder (can be removed)
│   └── README.md
│
├── docker-compose.yml                     # 🐳 Multi-container orchestration
├── .dockerignore                          # Docker build exclusions
├── .gitignore                             # Git exclusions
├── .env.example                           # Environment variables template
│
├── README.md                              # 📚 Complete documentation
├── QUICKSTART.md                          # 🚀 5-minute setup guide
└── BUILD_COMPLETE.md                      # ✅ Build summary & checklist

```

---

## 📊 Statistics

### Projects: 5

- NeuroScan.API (Web API)
- NeuroScan.Application (Services)
- NeuroScan.Domain (Entities)
- NeuroScan.Infrastructure (Data Access)
- NeuroScan.Shared (DTOs)

### Source Files: ~50

- Controllers: 3
- Services: 7
- Repositories: 4
- Entities: 5 (including BaseEntity)
- Interfaces: 10+

### Configuration Files: 7

- docker-compose.yml
- Dockerfile
- .dockerignore
- .gitignore
- .env.example
- appsettings.json
- launchSettings.json

### Documentation Files: 4

- README.md (comprehensive guide)
- QUICKSTART.md (quick start)
- BUILD_COMPLETE.md (status summary)
- PROJECT_STRUCTURE.md (this file)

---

## 🎯 Key Directories

### `/Controllers` - API Endpoints

Entry point for HTTP requests, handles routing and authorization

### `/Services` - Business Logic

Core application logic, orchestrates repositories and external services

### `/Repositories` - Data Access

Database operations with EF Core, implements repository pattern

### `/Entities` - Domain Models

Core business entities with relationships and validation rules

### `/Context` - Database Context

EF Core configuration, entity mappings, and migrations

---

## 🔗 Dependency Flow

```
API Layer (Controllers)
    ↓
Application Layer (Services)
    ↓
Domain Layer (Entities, Interfaces)
    ↑
Infrastructure Layer (Repositories, DbContext)
```

**No circular dependencies ✅**

---

## 📦 NuGet Packages Used

### NeuroScan.API

- Microsoft.AspNetCore.Authentication.JwtBearer (8.0)
- Swashbuckle.AspNetCore (6.5)
- Microsoft.EntityFrameworkCore.Design (8.0)

### NeuroScan.Application

- Microsoft.Extensions.Http (8.0)
- BCrypt.Net-Next (4.0.3)
- Microsoft.AspNetCore.Http.Abstractions (2.2.0)

### NeuroScan.Infrastructure

- Microsoft.EntityFrameworkCore (8.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0)
- Microsoft.EntityFrameworkCore.Tools (8.0)
- System.IdentityModel.Tokens.Jwt (8.0)

---

## 🚀 Build Commands

### Build Solution

```bash
cd C:\NeuroScan
dotnet build NeuroScan.sln
```

### Run API

```bash
cd NeuroScan.API
dotnet run
```

### Create Migration

```bash
cd NeuroScan.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../NeuroScan.API
```

### Apply Migration

```bash
dotnet ef database update --startup-project ../NeuroScan.API
```

### Run with Docker

```bash
docker-compose up --build
```

---

## 📝 File Naming Conventions

- **Controllers**: `{Entity}Controller.cs` (e.g., PatientController.cs)
- **Services**: `{Entity}Service.cs` (e.g., AuthService.cs)
- **Repositories**: `{Entity}Repository.cs` (e.g., UserRepository.cs)
- **Interfaces**: `I{Name}.cs` (e.g., IAuthService.cs)
- **Entities**: `{EntityName}.cs` (e.g., Patient.cs)
- **DTOs**: Include "DTO" suffix (e.g., PatientDTO.cs)

---

## 🏗️ Architecture Patterns

1. **Clean Architecture** - Layered dependencies
2. **Repository Pattern** - Data access abstraction
3. **Service Layer Pattern** - Business logic encapsulation
4. **Dependency Injection** - Loose coupling
5. **DTO Pattern** - Data transfer objects
6. **Soft Delete Pattern** - Data retention

---

**Project structure follows Clean Architecture and SOLID principles** ✅
