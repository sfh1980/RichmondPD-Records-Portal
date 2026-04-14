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
| Frontend   | Vanilla HTML/CSS/JS (IIS-served SPA)      |
| Seed/ETL   | Python 3, Faker, Requests                 |
| Testing    | Playwright UI tests                       |
| Dev Tools  | Visual Studio, SQL Server Mgmt Studio     |

---

## Project Structure

Most source files live at the project root (flat backend structure for a small interview demo).

```
RichmondPD - Records Portal/
├── Program.cs                  ← App startup, DI, middleware, auto-migration
├── AppDbContext.cs             ← EF Core DbContext + lookup seed data
├── Models.cs                   ← Domain models (Incident, Officer, Location, lookups)
├── Dtos.cs                     ← Request/response DTOs (C# records)
├── AuthController.cs           ← JWT login (hardcoded demo credentials)
├── IncidentsController.cs      ← CRUD + soft delete + pagination + filtering + dashboard + XML export
├── OfficersController.cs       ← CRUD + deactivate/reactivate
├── LocationsController.cs      ← CRUD with precinct filtering + incident count
├── ReportsController.cs        ← Stored-procedure reporting endpoints
├── PolicePortal.csproj         ← Project file (net8.0)
├── appsettings.json            ← Connection string, JWT config, logging
├── stored_procedures.sql       ← Trigger, 3 reporting stored procs, indexes
├── index.html                  ← Frontend shell (tabs, modals, tables)
├── styles.css                  ← Frontend styling
├── app.js                      ← Frontend SPA behavior
├── seed.py                     ← Python data seeder + report smoke checks
├── FRONTEND_OVERVIEW.md        ← Page-by-page frontend walkthrough
├── playwright.config.js        ← Playwright test configuration
├── tests/
│   └── ui/                     ← Login, incident, and officer UI tests
├── README.md
├── .github/
│   └── workflows/ci.yml        ← GitHub Actions restore/build workflow
└── .cursor/
    ├── mcp.json                ← Project-local MCP server config
    ├── rules/*.mdc             ← AI coding guidance
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
The ASP.NET Core app serves `index.html`, `styles.css`, and `app.js` directly from the project root, so opening `http://localhost:5000/` loads the frontend directly.

For IIS: create a site pointing to the project root.

**Demo credentials:** `admin` / `Password123!`

### 5. Run UI Tests
```bash
npm install
npx playwright install chromium
npm run test:ui
```

Useful variants:
- `npm run test:ui:headed`
- `npm run test:ui:debug`

---

## Key Features

### C# / .NET Web API
- RESTful controllers with proper HTTP verbs and status codes
- Dependency injection for DbContext and IConfiguration
- DTOs (C# records) to separate domain models from API responses
- Pagination with `X-Total-Count` header
- Server-side filtering by status, type, and precinct
- Incident soft delete with archive/restore workflow
- Officer deactivate/reactivate workflow

### SQL Server
- Normalized schema: Incidents, Officers, Locations, IncidentTypes, IncidentStatuses
- **Stored procedures:** `sp_GetOpenIncidentsByPrecinct`, `sp_MonthlyIncidentSummary`, `sp_OfficerWorkload`
- **Trigger:** `trg_Incidents_UpdatedAt` auto-sets timestamp on row update
- Indexes on StatusId, ReportedAt, and Officers.Precinct
- Incident archive state stored in the database via `IsDeleted` and `DeletedAt`

### Reporting
- `/api/reports/open-by-precinct` returns open incidents with officer and location details
- `/api/reports/monthly-summary` returns grouped monthly incident counts by type and status
- `/api/reports/officer-workload` returns officer incident workload totals from SQL Server stored procedures

### JWT Authentication
- Login endpoint issues signed tokens with claims (Name, Role, Jti)
- All API routes require `[Authorize]`
- Token stored in localStorage, sent as Bearer header

### Frontend SPA
- Tabbed workspace for incidents and officers
- Incident edit modal with archive/restore confirmation flow
- Officers management table with active/inactive filtering
- Frontend split into `index.html`, `styles.css`, and `app.js`

### XML Export
- `/api/incidents/export/xml` returns well-formed XML via `System.Xml.Linq`
- Filterable by status
- Frontend triggers file download

### UI Testing
- Playwright UI tests cover login, incident edit/archive/restore, and officer deactivate/reactivate
- `playwright.config.js` can start the API automatically for test runs

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
- Playwright UI suite passes (`3 passed`)

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
- **Frontend Overview:** [`FRONTEND_OVERVIEW.md`](FRONTEND_OVERVIEW.md) — explains the purpose of each visible section of the UI
- **AI Agents:** [`.cursor/agents/agents.md`](.cursor/agents/agents.md) — Cursor agent prompts for development workflows
- **Coding Rules:** [`.cursor/rules/`](.cursor/rules/) — project, .NET, and ASP.NET Core Cursor rule files
