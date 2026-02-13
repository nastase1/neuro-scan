# NeuroScan - Medical Imaging Analysis Platform

A comprehensive .NET 8.0 backend system for MRI brain scan analysis following Clean Architecture principles.

## 🎯 Overview

**NeuroScan** enables:

- Upload and storage of MRI brain scans (.nii files)
- Integration with Python AI microservice for tissue volume analysis
- OpenAI-powered medical report generation
- Doctor review workflow with human-in-the-loop corrections
- JWT-based authentication with role-based access control

## 🏗️ Architecture

### Clean Architecture Layers

```
NeuroScan/
├── NeuroScan.API/           # ASP.NET Core Web API (Presentation)
├── NeuroScan.Application/   # Business Logic & Services
├── NeuroScan.Domain/        # Core Entities & Interfaces
├── NeuroScan.Infrastructure/# EF Core, Repositories, External Services
├── NeuroScan.Shared/        # DTOs, Enums, Constants
└── docker-compose.yml       # Container Orchestration
```

### Technology Stack

- **.NET 8.0** - ASP.NET Core Web API
- **Entity Framework Core 8.0** - ORM with PostgreSQL
- **JWT Authentication** - Secure token-based auth
- **BCrypt** - Password hashing
- **Swagger/OpenAPI** - API documentation
- **Docker** - Containerization
- **PostgreSQL 16** - Database
- **FastAPI (Python)** - AI microservice
- **OpenAI API** - Medical report generation

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 16](https://www.postgresql.org/download/) (for local development)
- OpenAI API Key (optional, for report generation)

### Option 1: Run with Docker (Recommended)

1. **Clone and setup environment**

   ```bash
   cd C:\NeuroScan
   copy .env.example .env
   # Edit .env and add your OpenAI API key
   ```

2. **Start all services**

   ```bash
   docker-compose up --build
   ```

3. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Python AI Service: http://localhost:8000

### Option 2: Run Locally

1. **Set up PostgreSQL database**

   ```sql
   CREATE DATABASE neuroscan_db;
   CREATE USER neuroscan WITH PASSWORD 'neuroscan_secure_password';
   GRANT ALL PRIVILEGES ON DATABASE neuroscan_db TO neuroscan;
   ```

2. **Update connection string**

   Edit `NeuroScan.API/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=neuroscan_db;Username=neuroscan;Password=neuroscan_secure_password"
   }
   ```

3. **Run migrations**

   ```bash
   cd NeuroScan.Infrastructure
   dotnet ef migrations add InitialCreate --startup-project ../NeuroScan.API
   dotnet ef database update --startup-project ../NeuroScan.API
   ```

4. **Run the API**
   ```bash
   cd ../NeuroScan.API
   dotnet run
   ```

## 📊 Database Schema

### Entities

- **User** - Authentication and user management
  - Fields: Id, FirstName, LastName, Email, PasswordHash, Role
  - Roles: StandardUser, Doctor

- **Patient** - Patient records
  - Fields: Id, FirstName, LastName, DateOfBirth, MedicalRecordNumber
  - Relationships: CreatedBy (User)

- **MriScan** - Uploaded brain scans
  - Fields: Id, OriginalFileName, StoredFilePath, UploadDate, Status
  - Statuses: Uploaded, Processing, Analyzed, Failed, ReviewedByDoctor
  - Relationships: Patient, ReviewedByDoctor (User), AnalysisResult

- **AnalysisResult** - AI analysis results
  - Fields: Id, CsfVolume, GmVolume, WmVolume, AsymmetryIndex, MedicalReportText
  - Relationships: MriScan (1-to-1)

## 🔐 Authentication

### Default Test Accounts

After seeding, these accounts are available:

| Role          | Email                | Password  |
| ------------- | -------------------- | --------- |
| Doctor        | doctor@neuroscan.com | doctor123 |
| Standard User | user@neuroscan.com   | user123   |

### Authentication Flow

1. **Register**: POST `/api/auth/register`

   ```json
   {
     "firstName": "John",
     "lastName": "Doe",
     "email": "john@example.com",
     "password": "SecurePassword123",
     "role": 0
   }
   ```

2. **Login**: POST `/api/auth/login`

   ```json
   {
     "email": "john@example.com",
     "password": "SecurePassword123"
   }
   ```

3. **Use Token**: Add to request headers:
   ```
   Authorization: Bearer {your-jwt-token}
   ```

## 📡 API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### Patients

- `GET /api/patient` - Get all patients for current user
- `GET /api/patient/{id}` - Get patient by ID
- `POST /api/patient` - Create new patient
- `PUT /api/patient/{id}` - Update patient

### MRI Scans

- `POST /api/mriscan/upload` - Upload .nii scan file
- `GET /api/mriscan/{id}` - Get scan details with analysis
- `GET /api/mriscan/pending-review` - Get scans pending review (Doctor only)
- `POST /api/mriscan/{id}/correct-mask` - Submit corrected mask (Doctor only)

## 🧪 Testing the API

### Using Swagger UI

1. Navigate to http://localhost:5000/swagger
2. Click "Authorize" button
3. Login to get JWT token
4. Enter token in format: `Bearer {your-token}`
5. Test endpoints

### Using cURL

```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@neuroscan.com","password":"user123"}'

# Upload MRI Scan
curl -X POST http://localhost:5000/api/mriscan/upload \
  -H "Authorization: Bearer {your-token}" \
  -F "patientId={patient-guid}" \
  -F "file=@scan.nii"
```

## 🐍 Python AI Service

The AI microservice is a placeholder that returns mock data. To implement actual analysis:

1. Edit `python-ai-service/main.py`
2. Install required packages (nibabel, tensorflow/pytorch)
3. Load your trained segmentation model
4. Process .nii files and calculate tissue volumes

Example structure:

```python
import nibabel as nib
import numpy as np

# Load NIfTI file
img = nib.load(nii_path)
data = img.get_fdata()

# Run segmentation model
mask = your_model.predict(data)

# Calculate volumes
csf_volume = calculate_volume(mask, label=1)
gm_volume = calculate_volume(mask, label=2)
wm_volume = calculate_volume(mask, label=3)
```

## 🔧 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=neuroscan_db;..."
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong123456",
    "Issuer": "NeuroScanAPI",
    "Audience": "NeuroScanClient"
  },
  "PythonAiService": {
    "Url": "http://localhost:8000"
  },
  "OpenAI": {
    "ApiKey": "sk-your-key-here"
  },
  "Storage": {
    "UploadPath": "uploads/scans",
    "TrainingDataPath": "uploads/training-data"
  }
}
```

### Environment Variables (Docker)

Set in `docker-compose.yml` or `.env` file:

- `OPENAI_API_KEY` - Your OpenAI API key
- `ConnectionStrings__DefaultConnection` - Database connection
- `Jwt__Key` - JWT signing key

## 🗄️ Database Migrations

### Create New Migration

```bash
cd NeuroScan.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../NeuroScan.API
```

### Apply Migrations

```bash
dotnet ef database update --startup-project ../NeuroScan.API
```

### Rollback Migration

```bash
dotnet ef database update PreviousMigrationName --startup-project ../NeuroScan.API
```

## 📁 Project Structure

```
NeuroScan/
├── NeuroScan.API/
│   ├── Controllers/          # API endpoints
│   ├── Program.cs           # Startup configuration
│   ├── appsettings.json     # Configuration
│   └── Dockerfile           # Container definition
│
├── NeuroScan.Application/
│   ├── IServices/           # Service interfaces
│   └── Services/            # Service implementations
│
├── NeuroScan.Domain/
│   ├── Entities/            # Domain models
│   └── IRepositories/       # Repository interfaces
│
├── NeuroScan.Infrastructure/
│   ├── Context/             # DbContext
│   ├── Repositories/        # Repository implementations
│   ├── Services/            # External service integrations
│   └── Data/                # Database seeding
│
├── NeuroScan.Shared/        # DTOs and shared types
├── python-ai-service/       # AI microservice
│   └── main.py
└── docker-compose.yml       # Multi-container setup
```

## 🔒 Security Considerations

### Production Checklist

- [ ] Change JWT secret key
- [ ] Use strong PostgreSQL password
- [ ] Enable HTTPS/TLS
- [ ] Implement rate limiting
- [ ] Add input validation middleware
- [ ] Enable audit logging
- [ ] Implement HIPAA compliance measures
- [ ] Encrypt files at rest
- [ ] Add virus scanning for uploads
- [ ] Set up database backups
- [ ] Implement API versioning

### HIPAA Compliance Notes

This is a development version. For production use with PHI:

1. Enable encryption at rest and in transit
2. Implement comprehensive audit logging
3. Add access controls and user activity monitoring
4. Ensure BAA with cloud providers
5. Implement data retention policies
6. Add emergency access procedures

## 🐛 Troubleshooting

### Database Connection Issues

```bash
# Test PostgreSQL connection
docker exec -it neuroscan-db psql -U neuroscan -d neuroscan_db
```

### Migration Errors

```bash
# Drop and recreate database
dotnet ef database drop --startup-project ../NeuroScan.API
dotnet ef database update --startup-project ../NeuroScan.API
```

### Docker Issues

```bash
# Rebuild containers
docker-compose down -v
docker-compose up --build

# View logs
docker-compose logs api
docker-compose logs postgres
```

## 📚 Additional Resources

- [Clean Architecture Guide](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

## 📝 License

This project is for educational and development purposes.

## 🤝 Contributing

This is a reference implementation. For production use:

1. Implement comprehensive unit and integration tests
2. Add API rate limiting
3. Implement proper error handling and logging
4. Add health check endpoints
5. Set up CI/CD pipeline
6. Add performance monitoring

---

**Built with Clean Architecture principles for medical imaging analysis**
