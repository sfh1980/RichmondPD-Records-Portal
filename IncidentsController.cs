using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicePortal.Data;
using PolicePortal.DTOs;
using PolicePortal.Models;
using System.Xml.Linq;

namespace PolicePortal.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public IncidentsController(AppDbContext db) => _db = db;

    // ── GET /api/incidents ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidentDto>>> GetAll(
        [FromQuery] int?    statusId   = null,
        [FromQuery] int?    typeId     = null,
        [FromQuery] string? precinct   = null,
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 25)
    {
        var query = _db.Incidents
            .Include(i => i.IncidentType)
            .Include(i => i.IncidentStatus)
            .Include(i => i.Location)
            .Include(i => i.Officer)
            .AsQueryable();

        if (statusId.HasValue)  query = query.Where(i => i.IncidentStatusId == statusId);
        if (typeId.HasValue)    query = query.Where(i => i.IncidentTypeId == typeId);
        if (!string.IsNullOrEmpty(precinct))
            query = query.Where(i => i.Location.Precinct == precinct);

        var total = await query.CountAsync();
        Response.Headers["X-Total-Count"] = total.ToString();

        var items = await query
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return Ok(items);
    }

    // ── GET /api/incidents/{id} ────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<ActionResult<IncidentDto>> GetById(int id)
    {
        var incident = await _db.Incidents
            .Include(i => i.IncidentType)
            .Include(i => i.IncidentStatus)
            .Include(i => i.Location)
            .Include(i => i.Officer)
            .FirstOrDefaultAsync(i => i.Id == id);

        return incident is null ? NotFound() : Ok(MapToDto(incident));
    }

    // ── POST /api/incidents ────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<IncidentDto>> Create([FromBody] CreateIncidentRequest req)
    {
        var caseNumber = await GenerateCaseNumber();
        var incident = new Incident
        {
            CaseNumber      = caseNumber,
            Description     = req.Description,
            OccurredAt      = req.OccurredAt,
            IncidentTypeId  = req.IncidentTypeId,
            IncidentStatusId = req.IncidentStatusId,
            LocationId      = req.LocationId,
            OfficerId       = req.OfficerId
        };

        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        // Re-load with nav properties for response
        return await GetById(incident.Id);
    }

    // ── PUT /api/incidents/{id} ────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<ActionResult<IncidentDto>> Update(int id, [FromBody] UpdateIncidentRequest req)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        incident.Description      = req.Description;
        incident.OccurredAt       = req.OccurredAt;
        incident.IncidentTypeId   = req.IncidentTypeId;
        incident.IncidentStatusId = req.IncidentStatusId;
        incident.LocationId       = req.LocationId;
        incident.OfficerId        = req.OfficerId;
        // UpdatedAt is handled by the DB trigger (see SQL script)

        await _db.SaveChangesAsync();
        return await GetById(id);
    }

    // ── DELETE /api/incidents/{id} ─────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var incident = await _db.Incidents.FindAsync(id);
        if (incident is null) return NotFound();

        _db.Incidents.Remove(incident);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── GET /api/incidents/export/xml ──────────────────────────────────────────
    [HttpGet("export/xml")]
    public async Task<IActionResult> ExportXml([FromQuery] int? statusId = null)
    {
        var query = _db.Incidents
            .Include(i => i.IncidentType)
            .Include(i => i.IncidentStatus)
            .Include(i => i.Location)
            .Include(i => i.Officer)
            .AsQueryable();

        if (statusId.HasValue)
            query = query.Where(i => i.IncidentStatusId == statusId);

        var incidents = await query.OrderByDescending(i => i.ReportedAt).ToListAsync();

        var xml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("IncidentReport",
                new XAttribute("GeneratedAt", DateTime.UtcNow.ToString("o")),
                new XAttribute("TotalRecords", incidents.Count),
                incidents.Select(i =>
                    new XElement("Incident",
                        new XElement("CaseNumber",   i.CaseNumber),
                        new XElement("Type",         i.IncidentType.Name),
                        new XElement("Status",       i.IncidentStatus.Name),
                        new XElement("Description",  i.Description),
                        new XElement("ReportedAt",   i.ReportedAt.ToString("o")),
                        new XElement("OccurredAt",   i.OccurredAt?.ToString("o") ?? "Unknown"),
                        new XElement("Officer",
                            new XElement("Name",   $"{i.Officer.FirstName} {i.Officer.LastName}"),
                            new XElement("Badge",  i.Officer.BadgeNumber),
                            new XElement("Rank",   i.Officer.Rank)
                        ),
                        new XElement("Location",
                            new XElement("Street",   i.Location.Street),
                            new XElement("City",     i.Location.City),
                            new XElement("State",    i.Location.State),
                            new XElement("ZipCode",  i.Location.ZipCode),
                            new XElement("Precinct", i.Location.Precinct ?? "N/A")
                        )
                    )
                )
            )
        );

        return Content(xml.ToString(), "application/xml");
    }

    // ── GET /api/incidents/dashboard ───────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStats>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var stats = new DashboardStats(
            TotalIncidents:      await _db.Incidents.CountAsync(),
            OpenIncidents:       await _db.Incidents.CountAsync(i => i.IncidentStatus.Name == "Open"),
            ClosedThisMonth:     await _db.Incidents.CountAsync(i => i.IncidentStatus.Name == "Closed" && i.UpdatedAt >= startOfMonth),
            UnderInvestigation:  await _db.Incidents.CountAsync(i => i.IncidentStatus.Name == "Under Investigation"),
            ByType: await _db.Incidents
                .GroupBy(i => i.IncidentType.Name)
                .Select(g => new IncidentsByType(g.Key, g.Count()))
                .ToListAsync(),
            ByStatus: await _db.Incidents
                .GroupBy(i => new { i.IncidentStatus.Name, i.IncidentStatus.ColorHex })
                .Select(g => new IncidentsByStatus(g.Key.Name, g.Key.ColorHex, g.Count()))
                .ToListAsync()
        );

        return Ok(stats);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<string> GenerateCaseNumber()
    {
        var year  = DateTime.UtcNow.Year;
        var count = await _db.Incidents.CountAsync() + 1;
        return $"RVA-{year}-{count:D5}";
    }

    private static IncidentDto MapToDto(Incident i) => new(
        i.Id,
        i.CaseNumber,
        i.Description,
        i.ReportedAt,
        i.OccurredAt,
        i.UpdatedAt,
        i.IncidentType.Name,
        i.IncidentStatus.Name,
        i.IncidentStatus.ColorHex,
        $"{i.Officer.FirstName} {i.Officer.LastName}",
        i.Officer.BadgeNumber,
        $"{i.Location.Street}, {i.Location.City}, {i.Location.State} {i.Location.ZipCode}",
        i.Location.Precinct
    );
}
