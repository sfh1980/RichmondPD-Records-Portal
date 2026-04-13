# Richmond PD — Records Portal

A full-stack police incident management system built to demonstrate:
**C# / .NET Web API, MSSQL, Stored Procedures, Triggers, JWT Auth, XML Export, Python ETL, IIS Deployment, Git**

---

## Tech Stack

| Layer      | Technology                                |
|------------|-------------------------------------------|
| Backend    | ASP.NET Core 8, C#, Entity Framework Core |
| Database   | Microsoft SQL Server                      |
| Auth       | JWT Bearer tokens                         |
| Frontend   | Vanilla HTML/CSS/JS (IIS-served)          |
| Seed/ETL   | Python 3, Faker, Requests                 |
| Dev Tools  | Visual Studio, SQL Server Mgmt Studio     |

---

## Project Structure

All source files live at the project root (flat structure — no subfolders for a ~10-file demo).

```
RichmondPD - Records Portal/
├── Program.cs                  ← App startup, DI, middleware, auto-migration
├── AppDbContext.cs             ← EF Core DbContext + lookup seed data
├── Models.cs                   ← Domain models (Incident, Officer, Location, lookups)
├── Dtos.cs                     ← Request/response DTOs (C# records)
├── AuthController.cs           ← JWT login (hardcoded demo credentials)
├── IncidentsController.cs      ← CRUD + pagination + filtering + dashboard + XML export
├── OfficersController.cs       ← CRUD with soft delete
├── LocationsController.cs      ← CRUD with precinct filtering + incident count
├── ReportsController.cs        ← Stored-procedure reporting endpoints
├── PolicePortal.csproj         ← Project file (net8.0)
├── appsettings.json            ← Connection string, JWT config, logging
├── stored_procedures.sql       ← Trigger, 3 reporting stored procs, indexes
├── index.html                  ← Frontend SPA (login, dashboard, tables, XML export)
├── seed.py                     ← Python data seeder + report smoke checks
├── README.md
├── .github/
│   └── workflows/ci.yml        ← GitHub Actions restore/build workflow
└── .cursor/
    ├── rules/cursor.rules      ← AI coding guidance
    ├── agents/agents.md        ← Cursor agent prompts
    └── TECHNICAL_SPEC.md       ← Full technical documentation
```

---

## Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server Express / LocalDB / full SQL Server
- Python 3.9+ (for seeder)

### 1. Database Setup
```bash
dotnet tool restore
dotnet dotnet-ef database update
```
Then run `stored_procedures.sql` in SQL Server Management Studio or `sqlcmd` to create the trigger, stored procedures, and indexes.

The checked-in app settings are configured for a local SQL Server Express instance:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PolicePortal;Trusted_Connection=True;TrustServerCertificate=True;"
```

If your machine uses a different instance name, update `appsettings.json` before running the API.

### 2. Run the API
```bash
dotnet run
```
- Frontend: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### 3. Seed Data
```bash
pip install faker requests
python seed.py
```

The seeder creates locations, officers, and incidents through the API, then smoke-tests the report endpoints.

### 4. Frontend
The ASP.NET Core app now serves `index.html` from the project root, so opening `http://localhost:5000/` loads the frontend directly.

For IIS: create a site pointing to the project root.

**Demo credentials:** `admin` / `Password123!`

---

## Key Features

### C# / .NET Web API
- RESTful controllers with proper HTTP verbs and status codes
- Dependency injection for DbContext and IConfiguration
- DTOs (C# records) to separate domain models from API responses
- Pagination with `X-Total-Count` header
- Server-side filtering by status, type, and precinct

### SQL Server
- Normalized schema: Incidents, Officers, Locations, IncidentTypes, IncidentStatuses
- **Stored procedures:** `sp_GetOpenIncidentsByPrecinct`, `sp_MonthlyIncidentSummary`, `sp_OfficerWorkload`
- **Trigger:** `trg_Incidents_UpdatedAt` auto-sets timestamp on row update
- Indexes on StatusId, ReportedAt, and Officers.Precinct

### Reporting
- `/api/reports/open-by-precinct` returns open incidents with officer and location details
- `/api/reports/monthly-summary` returns grouped monthly incident counts by type and status
- `/api/reports/officer-workload` returns officer incident workload totals from SQL Server stored procedures

### JWT Authentication
- Login endpoint issues signed tokens with claims (Name, Role, Jti)
- All API routes require `[Authorize]`
- Token stored in localStorage, sent as Bearer header

### XML Export
- `/api/incidents/export/xml` returns well-formed XML via `System.Xml.Linq`
- Filterable by status
- Frontend triggers file download

### Python ETL
- `seed.py` generates fake locations, officers, and incidents via the API
- Smoke-tests the reporting endpoints after seeding
- Demonstrates Python + Requests + Faker

### CI
- GitHub Actions workflow restores and builds the project on pushes, pull requests, and manual runs

### Verified Locally
- API starts successfully on `http://localhost:5000`
- EF migrations apply cleanly to `localhost\\SQLEXPRESS`
- `stored_procedures.sql` applies cleanly after migrations
- `seed.py` completes and report endpoints return seeded data

### IIS Deployment
- Frontend is a static HTML file — drop into an IIS site
- API can be published and hosted in IIS with the ASP.NET Core Module

---

## Interview Talking Points

1. **"Walk me through the architecture"** — REST API + SPA. Frontend calls the Web API over HTTP, API uses EF Core to talk to SQL Server. Controllers inject DbContext directly — no service/repository layer since this is a focused demo. Stored procedures handle complex reporting queries.

2. **"Why stored procedures instead of just LINQ?"** — Stored procedures are pre-compiled and easier for DBAs to tune. Keeps complex reporting logic in the database where it belongs.

3. **"How does the trigger work?"** — `AFTER UPDATE` trigger on Incidents sets `UpdatedAt = GETUTCDATE()`. Keeps audit timestamps accurate without relying on application code.

4. **"How is auth handled?"** — JWT: user POSTs credentials, gets a signed token, includes it in subsequent requests as a Bearer header. Stateless — no session on the server.

5. **"What would you do differently in production?"** — Hash passwords (BCrypt), store users in a DB table, add refresh tokens, use HTTPS, move JWT key to Azure Key Vault, add service/repository layers, FluentValidation, global error handling middleware, and logging/telemetry.

---

## Documentation

- **Technical Spec:** [`.cursor/TECHNICAL_SPEC.md`](.cursor/TECHNICAL_SPEC.md) — full domain model, API reference, architecture decisions, progress tracker, and tech debt
- **AI Agents:** [`.cursor/agents/agents.md`](.cursor/agents/agents.md) — Cursor agent prompts for development workflows
- **Coding Rules:** [`.cursor/rules/cursor.rules`](.cursor/rules/cursor.rules) — AI coding behavior guidance for this project
