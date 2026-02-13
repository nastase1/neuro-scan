# NeuroScan Quick Start Guide

## 🚀 Getting Started in 5 Minutes

### Prerequisites Check

```bash
# Verify .NET 8.0 SDK
dotnet --version

# Verify Docker is running
docker --version
```

### Option 1: Local Development (No Docker)

1. **Install PostgreSQL** (if not already installed)
   - Download from: https://www.postgresql.org/download/
   - Default port: 5432

2. **Create Database**

   ```sql
   CREATE DATABASE neuroscan_db;
   CREATE USER neuroscan WITH PASSWORD 'neuroscan_secure_password';
   GRANT ALL PRIVILEGES ON DATABASE neuroscan_db TO neuroscan;
   ```

3. **Run Database Migrations**

   ```bash
   cd C:\NeuroScan\NeuroScan.Infrastructure
   dotnet ef migrations add InitialCreate --startup-project ../NeuroScan.API
   dotnet ef database update --startup-project ../NeuroScan.API
   ```

4. **Run the Application**

   ```bash
   cd ../NeuroScan.API
   dotnet run
   ```

5. **Access Swagger**
   - Open browser: http://localhost:5000/swagger

### Option 2: Docker (Recommended for Full Stack)

1. **Set OpenAI Key (Optional)**

   ```bash
   cd C:\NeuroScan
   copy .env.example .env
   # Edit .env and add: OPENAI_API_KEY=sk-your-key
   ```

2. **Start All Services**

   ```bash
   docker-compose up --build
   ```

3. **Access Services**
   - API Swagger: http://localhost:5000/swagger
   - Python AI: http://localhost:8000/docs
   - PostgreSQL: localhost:5432

## 🧪 Testing the API

### Step 1: Login

```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "user@neuroscan.com",
  "password": "user123"
}
```

Copy the `token` from the response.

### Step 2: Create a Patient

```bash
POST http://localhost:5000/api/patient
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: application/json

{
  "firstName": "Test",
  "lastName": "Patient",
  "dateOfBirth": "1990-01-01",
  "medicalRecordNumber": "MRN-TEST-001"
}
```

Copy the patient `id` from the response.

### Step 3: Upload MRI Scan (requires .nii file)

```bash
POST http://localhost:5000/api/mriscan/upload
Authorization: Bearer YOUR_TOKEN_HERE
Content-Type: multipart/form-data

patientId: PATIENT_GUID_HERE
file: scan.nii
```

### Step 4: Check Analysis Results

```bash
GET http://localhost:5000/api/mriscan/{scanId}
Authorization: Bearer YOUR_TOKEN_HERE
```

## 📊 Default Test Accounts

| Role   | Email                | Password  |
| ------ | -------------------- | --------- |
| Doctor | doctor@neuroscan.com | doctor123 |
| User   | user@neuroscan.com   | user123   |

## 🔧 Common Issues

### "Connection refused" to PostgreSQL

```bash
# Check if PostgreSQL is running
docker ps  # (if using Docker)
# or
sudo systemctl status postgresql  # (Linux)
# or
pg_isready -h localhost -p 5432  # (any platform)
```

### "JWT Key not configured"

- Check `appsettings.json` has a valid `Jwt:Key` (32+ characters)

### "Cannot read .nii file"

- Ensure you're uploading a valid NIfTI (.nii or .nii.gz) file
- File size limit: 500MB

### Database Migration Errors

```bash
# Reset database
cd NeuroScan.Infrastructure
dotnet ef database drop --startup-project ../NeuroScan.API --force
dotnet ef database update --startup-project ../NeuroScan.API
```

## 📁 Project Structure Summary

```
NeuroScan/
├── NeuroScan.API/              # Controllers, Program.cs
├── NeuroScan.Application/       # Services, Business Logic
├── NeuroScan.Domain/            # Entities, Interfaces
├── NeuroScan.Infrastructure/    # DbContext, Repositories
├── NeuroScan.Shared/            # DTOs
├── python-ai-service/           # AI Microservice
└── docker-compose.yml           # Container Setup
```

## 🔗 Next Steps

1. **Implement Real AI Model**
   - Edit `python-ai-service/main.py`
   - Add nibabel, tensorflow/pytorch
   - Load your trained segmentation model

2. **Add Frontend**
   - Angular 17+ recommended
   - Integrate with API using JWT tokens

3. **Production Deployment**
   - Change JWT secret
   - Use Azure/AWS PostgreSQL
   - Enable HTTPS
   - Add rate limiting

## 📚 Documentation

- Full README: [README.md](README.md)
- API Documentation: http://localhost:5000/swagger (when running)
- Architecture Guide: See README.md "Architecture" section

## 🆘 Support

For issues or questions:

1. Check [README.md](README.md) troubleshooting section
2. Review API error messages in console logs
3. Check Docker logs: `docker-compose logs api`

---

**You're ready to start! Run the application and test the endpoints.**
