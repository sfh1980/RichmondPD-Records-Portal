# RichmondPD Records Portal — Technical Specification

<!-- Single source of truth for all project technical documentation.
     .cursor/rules/*.mdc govern AI coding behavior.
     README.md is the public-facing overview.
     This file documents what IS built — not what will be. -->

---

## 1. Project Overview

**Project:** RichmondPD Records Portal
**Domain:** Law Enforcement Records Management (interview demo)
**Purpose:** A portfolio project demonstrating full-stack C#/.NET skills for a police department IT position. Showcases Web API development, SQL Server with stored procedures and triggers, JWT authentication, XML export, Python ETL, and IIS deployment readiness.
**Primary Users:** Demo — single hardcoded admin user

### Technology Stack

| Component        | Technology                  | Version |
|------------------|-----------------------------|---------|
| API Framework    | ASP.NET Core Web API        | 8.0     |
| Language         | C#                          | 12      |
| ORM              | Entity Framework Core       | 8.0.0   |
| Database         | Microsoft SQL Server        | SQLEXPRESS verified locally; localdb/full also supported |
| Auth             | JWT Bearer                  | via Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0 |
| API Docs         | Swashbuckle (Swagger)       | 6.5.0   |
| Frontend         | Vanilla HTML/CSS/JS SPA     | —       |
| Seeder           | Python 3 + Faker + Requests | —       |
| UI Testing       | Playwright                  | 1.59.1  |

### Architecture

REST API + SPA. Controllers access AppDbContext directly. No service layer, no repository layer. See `.cursor/rules/` for project-specific coding guidance.

---

## 2. Architecture Decisions

### ADR-1: Direct DbContext in Controllers
**Date:** 2026-04-06
**Status:** Accepted
**Context:** This is an interview demo project. Adding service and repository layers increases complexity without benefit at this scale and makes it harder to walk through in a 30-minute interview.
**Decision:** Controllers inject AppDbContext directly and handle queries, mutations, and DTO mapping inline.
**Alternatives Considered:** 3-tier with Services + Repositories (standard for production, overhead for a demo).
**Outcome:** Simple, readable controllers that can be explained end-to-end in an interview. Documented as a production upgrade path.

### ADR-2: Flat File Structure
**Date:** 2026-04-06
**Status:** Accepted
**Context:** The project has ~10 source files. Subfolders add navigation overhead without organizational benefit at this size.
**Decision:** All .cs files live at the project root alongside the .csproj.
**Alternatives Considered:** Nested folders (/Controllers, /Models, /DTOs, /Data). Would be appropriate if the project grows past ~15 source files.
**Outcome:** Every file visible at a glance.

### ADR-3: Hardcoded Demo Authentication
**Date:** 2026-04-06
**Status:** Accepted
**Context:** The interview focus is JWT mechanics (token issuance, validation, Bearer header flow), not user management.
**Decision:** Single hardcoded user (admin/Password123!) in AuthController. No Users table, no password hashing.
**Alternatives Considered:** Users table with BCrypt hashing (production pattern, documented as upgrade path).
**Outcome:** Minimal auth code that still demonstrates the full JWT flow.

### ADR-4: DB Trigger for UpdatedAt
**Date:** 2026-04-06
**Status:** Accepted
**Context:** Need to demonstrate SQL Server trigger knowledge for the interview.
**Decision:** AFTER UPDATE trigger on Incidents sets UpdatedAt = GETUTCDATE(). Application code does not set this field.
**Alternatives Considered:** Application-level audit via SaveChangesAsync override (more comprehensive, deferred).
**Outcome:** Demonstrates triggers as an interview talking point. Limited to Incidents table only.

---

## 3. Domain Model

### IncidentStatus (lookup)

| Field    | Type   | Nullable | Constraints | Notes |
|----------|--------|----------|-------------|-------|
| Id       | int    | No       | PK          | Seeded: 1-4 |
| Name     | string | No       |             | Open, Under Investigation, Closed, Pending Review |
| ColorHex | string | No       | Default: #6B7280 | Hex color for frontend badge |

### IncidentType (lookup)

| Field | Type   | Nullable | Constraints | Notes |
|-------|--------|----------|-------------|-------|
| Id    | int    | No       | PK          | Seeded: 1-7 |
| Name  | string | No       |             | Theft, Assault, Vandalism, Burglary, Traffic Incident, Disturbance, Fraud |

### Location

| Field    | Type   | Nullable | Constraints | Notes |
|----------|--------|----------|-------------|-------|
| Id       | int    | No       | PK          |       |
| Street   | string | No       |             |       |
| City     | string | No       |             |       |
| State    | string | No       | Default: "VA" |     |
| ZipCode  | string | No       |             |       |
| Precinct | string | No       |             |       |

### Officer

| Field       | Type   | Nullable | Constraints      | Notes |
|-------------|--------|----------|------------------|-------|
| Id          | int    | No       | PK               |       |
| BadgeNumber | string | No       | Unique, MaxLen 20 |      |
| FirstName   | string | No       |                  |       |
| LastName    | string | No       |                  |       |
| Rank        | string | No       |                  |       |
| Precinct    | string | No       |                  |       |
| IsActive    | bool   | No       | Default: true    | Soft delete flag |

### Incident

| Field            | Type     | Nullable | Constraints           | Notes |
|------------------|----------|----------|-----------------------|-------|
| Id               | int      | No       | PK                    |       |
| CaseNumber       | string   | No       | Unique, MaxLen 20     | Format: RVA-{year}-{seq} |
| Description      | string   | No       | MaxLen 2000           |       |
| ReportedAt       | DateTime | No       | Default: UtcNow       |       |
| OccurredAt       | DateTime | Yes      |                       |       |
| UpdatedAt        | DateTime | Yes      |                       | Set by DB trigger only |
| IsDeleted        | bool     | No       | Default: false        | Soft delete flag |
| DeletedAt        | DateTime | Yes      |                       | UTC archive timestamp |
| IncidentTypeId   | int      | No       | FK → IncidentType     | Restrict delete |
| IncidentStatusId | int      | No       | FK → IncidentStatus   | Restrict delete |
| LocationId       | int      | No       | FK → Location         | Restrict delete |
| OfficerId        | int      | No       | FK → Officer          | Restrict delete |

### Relationships
- Incident → IncidentType (many-to-one)
- Incident → IncidentStatus (many-to-one)
- Incident → Location (many-to-one)
- Incident → Officer (many-to-one)
- Location → Incidents (one-to-many)
- Officer → Incidents (one-to-many)

---

## 4. API Reference

Frontend root: http://localhost:5000/
Swagger UI: http://localhost:5000/swagger

### Auth

| Method | Route              | Auth     | Request DTO    | Response DTO   | Notes |
|--------|--------------------|----------|----------------|----------------|-------|
| POST   | /api/auth/login    | None     | LoginRequest   | LoginResponse  | Hardcoded demo credentials |

### Incidents

| Method | Route                       | Auth      | Request DTO            | Response DTO  | Notes |
|--------|-----------------------------|-----------|------------------------|---------------|-------|
| GET    | /api/incidents              | [Authorize] | query: statusId, typeId, precinct, includeArchived, page, pageSize | IncidentDto[] | X-Total-Count header |
| GET    | /api/incidents/{id}         | [Authorize] | query: includeArchived | IncidentDto   |       |
| POST   | /api/incidents              | [Authorize] | CreateIncidentRequest | IncidentDto   | Auto-generates CaseNumber |
| PUT    | /api/incidents/{id}         | [Authorize] | UpdateIncidentRequest | IncidentDto   | UpdatedAt set by trigger |
| DELETE | /api/incidents/{id}         | [Authorize] | —                    | 204           | Soft delete (archive) |
| POST   | /api/incidents/{id}/restore | [Authorize] | —                    | IncidentDto   | Clears archive state |
| GET    | /api/incidents/export/xml   | [Authorize] | query: statusId      | XML           | application/xml |
| GET    | /api/incidents/dashboard    | [Authorize] | —                    | DashboardStats |       |

### Officers

| Method | Route                | Auth      | Request DTO | Response DTO | Notes |
|--------|----------------------|-----------|-------------|--------------|-------|
| GET    | /api/officers        | [Authorize] | query: activeOnly | OfficerDto[] |       |
| GET    | /api/officers/{id}   | [Authorize] | —           | OfficerDto   |       |
| POST   | /api/officers        | [Authorize] | CreateOfficerRequest | OfficerDto |       |
| PUT    | /api/officers/{id}   | [Authorize] | UpdateOfficerRequest | OfficerDto |       |
| DELETE | /api/officers/{id}   | [Authorize] | —           | 204          | Soft delete (IsActive=false) |
| POST   | /api/officers/{id}/reactivate | [Authorize] | —   | OfficerDto   | Restores officer to active state |

### Locations

| Method | Route                 | Auth      | Request DTO     | Response DTO | Notes |
|--------|-----------------------|-----------|-----------------|--------------|-------|
| GET    | /api/locations        | [Authorize] | query: precinct | LocationDto[] |      |
| GET    | /api/locations/{id}   | [Authorize] | —               | LocationDto  |       |
| POST   | /api/locations        | [Authorize] | Location (model) | Location    | Accepts raw model |
| PUT    | /api/locations/{id}   | [Authorize] | Location (model) | 204         |      |
| DELETE | /api/locations/{id}   | [Authorize] | —               | 204          | Hard delete |

### Reports (Stored Procedure Endpoints)

| Method | Route                              | Auth      | Request DTO | Response DTO | Notes |
|--------|------------------------------------|-----------|-------------|--------------|-------|
| GET    | /api/reports/open-by-precinct      | [Authorize] | query: precinct | OpenIncidentByPrecinctResult[] | Calls sp_GetOpenIncidentsByPrecinct |
| GET    | /api/reports/monthly-summary       | [Authorize] | query: year, month | MonthlySummaryResult[] | Calls sp_MonthlyIncidentSummary |
| GET    | /api/reports/officer-workload      | [Authorize] | query: activeOnly | OfficerWorkloadResult[] | Calls sp_OfficerWorkload |

---

## 5. Security Model

**Authentication:** JWT Bearer tokens. Login via POST /api/auth/login with hardcoded credentials (admin / Password123!). Token lifetime: 60 minutes. Claims: Name, Role ("Officer"), Jti.

**Authorization:** [Authorize] attribute on all controller classes except AuthController. Single role, no policies.

**CORS:** AllowAnyOrigin (demo convenience).

---

## 6. Audit and Data Integrity

**DB Trigger:** `trg_Incidents_UpdatedAt` — AFTER UPDATE on Incidents, sets UpdatedAt = GETUTCDATE().

**Soft delete:** Officers use `IsActive = false`. Incidents use `IsDeleted = true` plus `DeletedAt`.

No application-level audit trail.

---

## 7. Configuration Reference

| Key                          | Section           | Type   | Required | Default | Source        | Purpose |
|------------------------------|-------------------|--------|----------|---------|---------------|---------|
| DefaultConnection            | ConnectionStrings | string | Yes      | —       | appsettings   | SQL Server connection |
| Key                          | Jwt               | string | Yes      | fallback in code | appsettings | JWT signing key |
| Issuer                       | Jwt               | string | Yes      | —       | appsettings   | JWT issuer claim |
| Audience                     | Jwt               | string | Yes      | —       | appsettings   | JWT audience claim |
| ExpiryMinutes                | Jwt               | int    | No       | 60      | appsettings   | Token lifetime |
| LogLevel:Default             | Logging           | string | No       | Information | appsettings | Default log level |
| LogLevel:Microsoft.AspNetCore | Logging          | string | No       | Warning | appsettings   | ASP.NET Core log level |

---

## 8. Progress Tracker

| Feature                              | Status       | Notes |
|--------------------------------------|--------------|-------|
| Project scaffolding                  | Complete     | net8.0, flat structure |
| EF migrations                        | Complete     | `InitialCreate` and `LimitPrecinctLengths` checked in under `Migrations/` |
| Domain models (5 entities)           | Complete     | Models.cs |
| AppDbContext + seed data             | Complete     | Lookup tables seeded |
| JWT authentication                   | Complete     | Hardcoded demo creds |
| Swagger/OpenAPI                      | Complete     | Bearer security definition |
| Incident CRUD + pagination/filtering | Complete     | IncidentsController |
| Incident soft delete + restore       | Complete     | Archived incidents hidden by default, restore endpoint available |
| Dashboard stats endpoint             | Complete     | /api/incidents/dashboard |
| XML export endpoint                  | Complete     | /api/incidents/export/xml |
| Officer CRUD + soft delete           | Complete     | OfficersController |
| Officer reactivate                   | Complete     | `/api/officers/{id}/reactivate` |
| Location CRUD                        | Complete     | LocationsController |
| DB trigger (UpdatedAt)               | Complete     | stored_procedures.sql |
| Stored procedures (SQL)              | Complete     | 3 reporting procs + indexes |
| Frontend SPA                         | Complete     | `index.html`, `styles.css`, `app.js` |
| Frontend documentation               | Complete     | `FRONTEND_OVERVIEW.md` |
| Playwright UI tests                  | Complete     | Login, incidents, and officers UI flows |
| Python seeder                        | Complete     | Seeds locations, officers, incidents, then smoke-tests report endpoints |
| ReportsController (SP endpoints)     | Complete     | 3 GET endpoints calling stored procedures via ADO.NET |
| stored_procedures.sql cleanup        | Complete     | Removed dead Report CRUD procs; index creation is valid SQL Server syntax |
| Models.cs / Dtos.cs cleanup          | Complete     | Removed stale Report CRUD DTOs; added reporting result DTOs |
| CI workflow                          | Complete     | `.github/workflows/ci.yml` restores and builds the project |
| Database bootstrap verification      | Complete     | Verified against `localhost\\SQLEXPRESS` with schema, trigger, procs, and indexes applied |
| Runtime smoke testing                | Complete     | Login, dashboard, report endpoints, and `seed.py` verified locally |
| Frontend static hosting              | Complete     | `index.html` is served from the ASP.NET Core app root at `http://localhost:5000/` |
| IIS deployment                       | Not Started  |       |
| Git repo initialization              | Not Started  |       |

---

## 9. Technical Debt

1. **Hardcoded credentials** — admin/Password123! in AuthController. Deferred: demo project. Fix: Users table + BCrypt hashing.
2. **AllowAnyOrigin CORS** — Wide-open CORS policy. Deferred: no deployed frontend yet. Fix: explicit origin whitelist from config.
3. **JWT key in appsettings.json** — Secret committed to source. Deferred: demo project. Fix: .NET Secret Manager for dev, env vars or Key Vault for prod.
4. **No password hashing** — Plaintext comparison. Deferred: demo project. Fix: BCrypt.Net-Next.
5. **Locations still use hard delete** — `DbSet.Remove()` is still used for locations. Fix: add archive behavior if historical retention is needed there too.
6. **Locations POST/PUT accept raw domain model** — No request DTO, risk of over-posting. Fix: dedicated CreateLocationRequest / UpdateLocationRequest DTOs.
7. **No input validation beyond data annotations** — Improved, but still basic. Fix: FluentValidation.
8. **No error handling middleware** — Default ASP.NET Core responses. Fix: global exception middleware returning ProblemDetails.
9. **CaseNumber generation not concurrency-safe** — Uses COUNT(*)+1. Fix: database sequence or Hi-Lo pattern.
10. **IConfiguration read directly** — AuthController reads config[\"Jwt:Key\"]. Fix: IOptions\<JwtSettings\>.
11. **Seeder assumes API is already running** — `seed.py` does not start the app for you. Fix: optional automation script or dev container.
12. **Report endpoints depend on stored procedures existing** — API routes compile, but DB objects must be applied from `stored_procedures.sql`. Fix: document/enforce SQL bootstrap order or automate it.
13. **Startup assumes SQL Server is reachable** — `Program.cs` runs `Database.Migrate()` on boot. Fix: make startup migration optional per environment or add clearer bootstrap checks/logging.

---

## 10. Changelog

## [Unreleased]
### Added
- Initial project scaffold (net8.0, EF Core 8.0, flat structure)
- Domain models: Incident, Officer, Location, IncidentStatus, IncidentType
- AppDbContext with fluent API config and lookup seed data
- AuthController with JWT login (demo credentials)
- IncidentsController: CRUD, pagination, filtering, dashboard stats, XML export
- OfficersController: CRUD with soft delete
- LocationsController: CRUD with precinct filtering and incident count
- stored_procedures.sql: trigger, 3 reporting procs, indexes
- Frontend SPA (index.html) with login, dashboard, table, XML export
- Python seeder (seed.py) structure
- TECHNICAL_SPEC.md populated with real project data
- ReportsController stored-procedure endpoints with result DTOs
- EF migrations for initial schema creation and precinct length constraints
- GitHub Actions CI workflow for restore/build
- Cursor `.mdc` rule files for project, .NET, and ASP.NET Core guidance
- Project-local MCP config for Playwright and Snyk
- Incident archive/restore workflow with soft-delete DB fields
- Frontend split into `index.html`, `styles.css`, and `app.js`
- Officers tab with deactivate/reactivate workflow
- `FRONTEND_OVERVIEW.md` frontend walkthrough
- Playwright UI suite for login, incidents, and officers

### Changed
- `appsettings.json` now targets `localhost\\SQLEXPRESS` for local development
- `stored_procedures.sql` cleaned up, validated against SQL Server Express, and updated to exclude archived incidents
- `seed.py` now seeds locations/officers/incidents through the API and smoke-tests report endpoints
- `README.md` updated for split frontend assets, testing, and documentation links

---

*Last updated: 2026-04-14*
