# NeuroScan

**Platformă web de analiză MRI cerebrală cu inteligență artificială** — detectează tumori, evaluează riscul de epilepsie și segmentează anatomia cerebrală în aproximativ 2 minute.

Lucrare de licență — Universitatea Transilvania din Brașov, Facultatea de Matematică și Informatică, 2026  
Autor: **Năstase Teodor** | Coordonator: Lect. univ. dr. Vlad Monescu

---

## Cuprins

- [Descriere](#descriere)
- [Arhitectură](#arhitectură)
- [Stack tehnologic](#stack-tehnologic)
- [Cerințe preliminare](#cerințe-preliminare)
- [Pornire rapidă cu Docker](#pornire-rapidă-cu-docker)
- [Configurare manuală (dezvoltare)](#configurare-manuală-dezvoltare)
- [Variabile de mediu](#variabile-de-mediu)
- [API — endpoints principale](#api--endpoints-principale)
- [Modele AI](#modele-ai)
- [Structura proiectului](#structura-proiectului)
- [Performanță](#performanță)

---

## Descriere

NeuroScan permite medicilor să încarce scanări IRM cerebrale în format NIfTI (`.nii`, `.nii.gz`) și să primească automat:

- **Segmentare anatomică** — volum CSF, substanță cenușie (GM), substanță albă (WM) în cm³
- **Detecție tumori** — masca tumorală, volum și suprafață (cu 4 modalități BraTS: T1, T1ce, T2, FLAIR)
- **Scor de risc epilepsie** — algoritm multi-factorial pe scală 0–100
- **Biomarkeri avansați** — grosime cortex, densitate substanță albă, indice de asimetrie
- **Vizualizare slice-by-slice** — imagini axiale cu overlay colorate pentru segmentare și tumori
- **Rapoarte medicale automate** — generate cu GPT-4 sau template-based (fallback)

---

## Arhitectură

```
┌─────────────────────────────────────────────────────┐
│                     Utilizator                       │
└───────────────────────┬─────────────────────────────┘
                        │ HTTP / Browser
┌───────────────────────▼─────────────────────────────┐
│           Frontend  ·  Angular 20  ·  :4200          │
│           Tailwind CSS · TypeScript · Signals        │
└───────────────────────┬─────────────────────────────┘
                        │ REST API (JWT)
┌───────────────────────▼─────────────────────────────┐
│         Backend API  ·  ASP.NET Core 9  ·  :5000     │
│         Entity Framework · SQLite · BCrypt · SMTP    │
└───────────────────────┬─────────────────────────────┘
                        │ HTTP
┌───────────────────────▼─────────────────────────────┐
│      Serviciu AI  ·  Python FastAPI  ·  :8000        │
│      PyTorch 2.6 · MONAI 1.5 · SegResNet · BraTS    │
└─────────────────────────────────────────────────────┘
```

Toate cele trei servicii sunt containerizate cu Docker și orchestrate prin Docker Compose.

---

## Stack tehnologic

| Nivel | Tehnologie | Versiune |
|---|---|---|
| Frontend | Angular | 20 |
| Frontend | Tailwind CSS | 3.x |
| Backend | ASP.NET Core | 9.0 |
| Backend | Entity Framework Core | 8.x |
| Backend | SQLite | — |
| Backend | JWT Bearer | — |
| Backend | BCrypt.Net | — |
| AI Service | Python | 3.10 |
| AI Service | FastAPI | ≥ 0.109 |
| AI Service | PyTorch | 2.6.0 |
| AI Service | MONAI | ≥ 1.3 |
| AI Service | nibabel | ≥ 5.2 |
| Deployment | Docker + Docker Compose | — |

---

## Cerințe preliminare

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (cu Docker Compose v2)
- Opțional pentru dezvoltare:
  - Node.js ≥ 20 și Angular CLI (`npm install -g @angular/cli`)
  - .NET SDK 9.0
  - Python 3.10+

---

## Pornire rapidă cu Docker

```bash
# 1. Clonează repository-ul
git clone <repo-url>
cd NeuroScan

# 2. Configurează variabilele de mediu
cp .env.example .env
# editează .env cu cheile tale (JWT, SMTP, OpenAI)

# 3. Pornește toate serviciile
cd neuro-scan
docker compose up -d

# 4. Verifică că totul rulează
docker compose ps
```

| Serviciu | URL |
|---|---|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| AI Service | http://localhost:8000 |
| AI Docs | http://localhost:8000/docs |

> **Notă:** La prima pornire, backend-ul aplică automat migrațiile EF Core și creează baza de date SQLite.

### Oprire

```bash
docker compose down          # oprește containerele
docker compose down -v       # oprește și șterge volumele (date!)
```

### Acces public prin Cloudflare Tunnel (opțional)

```bash
# Setează CLOUDFLARE_TUNNEL_TOKEN în .env, apoi:
docker compose --profile public up -d
```

---

## Configurare manuală (dezvoltare)

### 1. Frontend — Angular

```bash
cd neuro-scan/neuro-scan-frontend
npm install
ng serve
# disponibil la http://localhost:4200
```

### 2. Backend — ASP.NET Core

```bash
cd neuro-scan
dotnet restore
dotnet ef database update --project NeuroScan.Infrastructure --startup-project NeuroScan.API
dotnet run --project NeuroScan.API
# disponibil la http://localhost:5000
```

### 3. Serviciu AI — Python FastAPI

```bash
cd python-ai-service

# Creează environment virtual
python -m venv .venv
.venv\Scripts\activate        # Windows
# sau: source .venv/bin/activate  # Linux/Mac

pip install -r requirements.txt

# Asigură-te că modelele .pth sunt în python-ai-service/models/
python app.py
# disponibil la http://localhost:8000
```

> **Modele AI:** Fișierele `.pth` nu sunt incluse în repository (dimensiune mare). Vezi secțiunea [Modele AI](#modele-ai).

---

## Variabile de mediu

Creează un fișier `.env` în directorul `neuro-scan/`:

```env
# JWT
JWT_KEY=YourSuperSecretKeyThatIsAtLeast32CharactersLong123456
JWT_ISSUER=NeuroScanAPI
JWT_AUDIENCE=NeuroScanClient

# Baza de date
CONNECTION_STRING=Data Source=/app/data/neuroscan.db

# Serviciu AI
PYTHON_AI_URL=http://python-ai:8000

# OpenAI (opțional — fallback template dacă lipsește)
OPENAI_API_KEY=sk-...

# Email SMTP (opțional)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM_EMAIL=your@gmail.com
SMTP_FROM_NAME=NeuroScan

# Cloudflare Tunnel (opțional)
CLOUDFLARE_TUNNEL_TOKEN=your-tunnel-token
```

---

## API — endpoints principale

Documentația completă este disponibilă la `/swagger` când backend-ul rulează.

| Endpoint | Metodă | Descriere |
|---|---|---|
| `/api/auth/register` | POST | Înregistrare utilizator nou |
| `/api/auth/login` | POST | Autentificare, returnează JWT |
| `/api/auth/reset-password` | POST | Resetare parolă prin cod email |
| `/api/patient` | GET/POST | Lista pacienți / adaugă pacient |
| `/api/patient/{id}` | GET/PUT/DELETE | Detalii / editare / ștergere pacient |
| `/api/mriscan/upload` | POST | Upload fișier NIfTI (single-channel) |
| `/api/mriscan/{id}/results` | GET | Rezultatele analizei AI |
| `/api/mriscan/{id}/status` | GET | Statusul procesării (polling) |
| `/api/admin/stats` | GET | Statistici globale (Admin) |

**Endpoints AI Service (FastAPI):**

| Endpoint | Metodă | Descriere |
|---|---|---|
| `/analyze` | POST | Analiză single-channel (segmentare + epilepsie) |
| `/analyze-tumor` | POST | Analiză 4-channel BraTS (T1, T1ce, T2, FLAIR) |
| `/raw-slices` | POST | Generare slice-uri grayscale |
| `/analyze-3d` | POST | Mesh 3D pentru vizualizare |
| `/health` | GET | Status serviciu și modele încărcate |

---

## Modele AI

Modelele antrenate nu sunt incluse în repository. Plasează fișierele `.pth` în `python-ai-service/models/`:

| Fișier | Descriere |
|---|---|
| `best_anatomy_model.pth` | SegResNet — segmentare anatomică (CSF/GM/WM) |
| `best_anatomy_model_SegResNet.pth` | Variantă alternativă segmentare |
| `SegResNet_BraTS_FULL_best.pth` | SegResNet — detecție tumori (BraTS 4-channel) |

**Performanță modele:**

| Model | Metric | Valoare |
|---|---|---|
| Segmentare anatomică (GM) | Dice Score | 0.847 |
| Segmentare anatomică (WM) | Dice Score | 0.891 |
| Detecție tumori (4-channel) | Dice Score | 0.880 |
| Detecție tumori (1-channel) | Dice Score | 0.67–0.71 |

---

## Structura proiectului

```
NeuroScan/
├── neuro-scan/                         # Soluție .NET + Frontend Angular
│   ├── docker-compose.yml
│   ├── NeuroScan.API/                  # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   └── Dockerfile
│   ├── NeuroScan.Application/          # Business logic, servicii
│   ├── NeuroScan.Domain/               # Entități, interfețe
│   ├── NeuroScan.Infrastructure/       # EF Core, migrații, email
│   ├── NeuroScan.Shared/               # DTO-uri comune
│   ├── NeuroScan.Tests/                # Teste xUnit
│   └── neuro-scan-frontend/            # Angular 20
│       ├── src/app/
│       │   ├── components/
│       │   ├── services/
│       │   └── guards/
│       └── Dockerfile
│
├── python-ai-service/                  # FastAPI + PyTorch AI Service
│   ├── app.py                          # Endpoints FastAPI
│   ├── inference.py                    # SegResNet inference
│   ├── volume_analyzer.py              # Analiză volumetrică + risc epilepsie
│   ├── model_loader.py                 # Încărcare modele la startup
│   ├── models/                         # Fișiere .pth (neincuse în repo)
│   ├── requirements.txt
│   └── Dockerfile
│
├── AI-TRAINING-CODES/                  # Scripturi antrenare modele
├── test-data/                          # Fișiere NIfTI pentru testare
└── licenta-capitol1.tex                # Documentație lucrare licență
```

---

## Performanță

**Timpii de răspuns API** (măsurați cu Postman Runner, 50 iterații, Docker local):

| Endpoint | Timp mediu | Timp maxim |
|---|---|---|
| POST /api/auth/login | 280 ms | 420 ms |
| GET /api/patient | 45 ms | 120 ms |
| POST /api/mriscan/upload (147 MB) | 2.1 s | 3.4 s |
| GET /api/mriscan/{id}/results | 18 ms | 65 ms |

**Timpii de procesare AI** (volum 182×218×182 voxeli, fișier 147 MB):

| Etapă | CPU (i7-10th gen) | GPU (RTX 3060) |
|---|---|---|
| Preprocesare + inferență segmentare | 75–90 s | 12–18 s |
| Calcul biomarkeri + risc epilepsie | 2–3 s | 2–3 s |
| Generare slice-uri vizualizare | 15–20 s | 15–20 s |
| **Total (fără detecție tumori)** | **~105 s** | **~40 s** |
| **Total (cu detecție tumori BraTS)** | **~150 s** | **~60 s** |

> Timpii AI sunt logați în consolă la fiecare procesare în formatul:
> `⏱️ [CPU] Stage times — Inference: 82.3s | Biomarkers: 2.1s | Slices: 17.4s | TOTAL: 101.8s`

---

## Roluri utilizatori

| Rol | Permisiuni |
|---|---|
| **Admin** | Gestionare utilizatori, statistici globale, asignare pacienți |
| **Doctor** | Creare/gestionare pacienți proprii, upload scanări, vizualizare rezultate |
| **Patient** | Vizualizare propriile scanări și rezultate |

---

*Licență: proiect academic — Universitatea Transilvania din Brașov, 2026*
