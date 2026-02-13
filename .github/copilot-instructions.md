# ICDCPTCoder - AI Coding Guidelines

## Project Overview
**ICDCPTCoder** is an ASP.NET Core 10 web application for managing patient visit records and doctor notes. The backend uses PostgreSQL with Entity Framework Core, following a layered architecture with clear separation of concerns.

## Architecture

### Layered Structure
- **Endpoints** (`Backend/Endpoints/`): Minimal API route definitions using extension methods for testability
- **Service** (`Backend/Service/`): Business logic layer with repository pattern (e.g., `PatientService` implements `IPatientService`)
- **Data** (`Backend/Data/`): Entity Framework Core `DbContext` and migrations
- **Contracts** (`Backend/Contracts/`): Interfaces, DTOs, and request/response models
- **Models** (`Backend/Models/`): Domain models (e.g., `PatientVisits`) used by EF Core

### Data Flow Pattern
```
HTTP Request → Endpoint (minimal API) → Service Interface → Service Implementation → DbContext → PostgreSQL
```

## Key Patterns

### 1. Endpoint Registration (Extension Methods)
Endpoints are registered as static extension methods on `IEndpointRouteBuilder` and called from `Program.cs`:
- Example: `app.MapPatientVisitsEndpoint()` in Backend/Program.cs
- File: Backend/Endpoints/PatientVisitsEndpoint.cs
- **When adding endpoints**: Create new file in `Endpoints/` with pattern `[Feature]Endpoint.cs`, define static method, register in `Program.cs`

### 2. Service Layer with Dependency Injection
Services are registered in `Program.cs` using scoped lifetime:
- `builder.Services.AddScoped<IPatientService, PatientService>()` 
- Services inject `AppDbContext` and use async/await patterns
- **DTOs**: Use `*ResponseDTO` for API responses, `*RequestDTO` for inputs (see Backend/Contracts/ResponseDTO/)

### 3. Database Access
- Single `DbContext` in Backend/Data/AppDbContext.cs exposes `DbSet<PatientVisits>`
- EF Core handles SQL generation; use LINQ queries
- Async operations only: `.ToListAsync()`, `.FirstOrDefaultAsync()`, etc.
- **Ordering convention**: Results ordered by `VisitDate` descending (see `GetPatientVisits`)

### 4. Migrations
- Use `dotnet ef migrations add [MigrationName]` when models change
- Migrations run automatically at startup unless app detects migration command line args
- Recent examples: AddVisitDateToPatientVisits migration in Backend/Migrations/

## Build & Run

### Required Setup
1. PostgreSQL running on `localhost:5432` with credentials from Backend/appsettings.json
2. Database name: `PatientVisits`
3. Run migrations on startup automatically

### Build Commands
```powershell
cd Backend
dotnet build
dotnet run
```

### Database Migrations
```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Project Conventions

### Naming
- **Models**: PascalCase, singular (e.g., `PatientVisits` - note: collection-like name but single entity)
- **DTOs**: `[Feature]RequestDTO` / `[Feature]ResponseDTO`
- **Services**: `[Feature]Service` implementing `I[Feature]Service`
- **Endpoints**: Static class `[Feature]Endpoint` with method `Map[Feature]Endpoint()`

### File Organization
- Do NOT create nested feature folders; keep flat structure (Endpoints/, Service/, Models/, etc.)
- Related DTOs stay together in `Contracts/ResponseDTO/` and `Contracts/RequestDTO/`
- Interfaces grouped in `Contracts/Interfaces/`

### DateTime Handling
- Always use `DateTime.UtcNow` for timestamps (see Backend/Models/PatientVisits.cs)
- Store without timezone info (database agnostic)

## Common Tasks

### Adding a New API Endpoint
1. Create interface in `Contracts/Interfaces/I[Feature]Service.cs`
2. Create request/response DTOs in `Contracts/`
3. Implement service in `Service/[Feature]Service.cs`
4. Create endpoint in `Endpoints/[Feature]Endpoint.cs` as static extension method
5. Register in `Program.cs`: `builder.Services.AddScoped<I[Feature]Service, [Feature]Service>();` and `app.Map[Feature]Endpoint();`

### Modifying Models
1. Update model in `Models/[Model].cs`
2. Run `dotnet ef migrations add [Description]`
3. Migrations auto-run on app startup

### Error Handling
- Minimal APIs use `Results.*` methods (e.g., `Results.Ok()`, `Results.BadRequest()`)
- No explicit exception handling yet; ensure services return expected DTO types

## Technology Stack
- **.NET**: 10.0 (latest)
- **Database**: PostgreSQL 10.0 via Npgsql
- **Web**: ASP.NET Core Minimal APIs (no Controllers)
- **ORM**: Entity Framework Core 10.0.2
- **Nullability**: Enabled (`<Nullable>enable</Nullable>`)
- **C# Features**: Implicit usings, top-level statements, records for DTOs

## Repository Pattern (Partial Implementation)
- `IAddDoctorsNoteRepository` / `AddDoctorsNoteRepository` exist but usage varies by feature
- PatientService uses `DbContext` directly for simple read-only queries
- AddDoctorsNoteService uses repository for CRUD isolation (better for complex update logic)
- **Decision rule**: Use repository for update/delete operations; use DbContext directly for queries

## Advanced Patterns

### 1. Conflict Resolution via Status Enums
The `AddDoctorsNote` endpoint demonstrates structured response handling:
- Request includes `ForceUpdate` flag to control overwrite behavior
- Response uses `VisitNoteStatus` enum (Created, Updated, Conflict)
- Endpoint maps status to HTTP responses: Conflict → 409, Updated → 200 Ok, Created → 201 Created
- **Files**: Backend/Contracts/ResponseDTO/AddDoctorsNoteResponseDTO.cs, Backend/Endpoints/AddDoctorsNote.cs

### 2. Request DTOs with Behavior Flags
- `AddDoctorsNoteRequestDTO.ForceUpdate` demonstrates DTOs carrying behavioral flags, not just data
- Service checks flag to decide business logic: allow conflict? reject? Pattern enables clients to control behavior
- Files: Backend/Contracts/RequestDTO/AddDoctorsNoteRequestDTO.cs, Backend/Service/AddDoctorsNoteService.cs (lines 15-35)

### 3. Conditional Service Registration
- Program.cs checks `Environment.GetCommandLineArgs()` to skip service registration during EF migrations
- Prevents unnecessary initialization when running `dotnet ef migrations add`

### 4. Comprehensive PHI (Protected Health Information) Redaction
The `PhiRedactionService` automatically detects and redacts sensitive medical data:

**Structured Data Redaction:** SSN, Patient/Visit/Insurance IDs, Phone, Email, Address, DOB, Driver's License, Passport

**Clinical Data Redaction:** Diagnoses (ICD-10), Medications (by name), Lab values, Mental health, Substance use, Genetic/Reproductive info, HIV/AIDS, Rare diseases

**Contextual NER Domain Rules:**
- "wife Linda" → `[FAMILY_MEMBER REDACTED]`
- "works at Google" → `[EMPLOYER REDACTED]`
- "lives in Sunnyvale" → `[LOCATION REDACTED]`
- "45 years old" → `[AGE REDACTED]`

**Integration:** `PatientService` uses `IPhiRedactionService` to redact all doctor notes. Response includes `PhiRedacted: bool` to signal frontend to show privacy notice.

**Files:** Backend/Service/PhiRedactionService.cs (158 regex + NER patterns), Backend/Service/PatientService.cs, Backend/Contracts/ResponseDTO/PatientVisitsResponseDTO.cs

## Frontend Layer
- Razor Pages in Backend/Pages/ (DoctorsNote.cshtml, PatientVisits.cshtml) kept separate from API
- Not yet integrated with API endpoints; serves as alternative read path
- Display privacy notice when `PhiRedacted: true` received

