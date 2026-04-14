namespace PolicePortal.Models;

// ═══════════════════════════════════════════════════════════════════════════════
// Lookup / reference tables
// ═══════════════════════════════════════════════════════════════════════════════

// ─── Incidents ─────────────────────────────────────────────────────────────────

public class IncidentStatus
{
    public int Id          { get; set; }
    public string Name     { get; set; } = string.Empty;   // Open, Closed, Under Investigation, …
    public string ColorHex { get; set; } = "#6B7280";
}

public class IncidentType
{
    public int Id      { get; set; }
    public string Name { get; set; } = string.Empty;       // Theft, Assault, Vandalism, …
}

// ═══════════════════════════════════════════════════════════════════════════════
// Core domain
// ═══════════════════════════════════════════════════════════════════════════════

public class Location
{
    public int    Id          { get; set; }
    public string Street      { get; set; } = string.Empty;
    public string City        { get; set; } = string.Empty;
    public string State       { get; set; } = "VA";
    public string ZipCode     { get; set; } = string.Empty;
    public string Precinct    { get; set; } = string.Empty;

    // Navigation
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}

public class Officer
{
    public int    Id          { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string FirstName   { get; set; } = string.Empty;
    public string LastName    { get; set; } = string.Empty;
    public string Rank        { get; set; } = string.Empty;
    public string Precinct    { get; set; } = string.Empty;
    public bool   IsActive    { get; set; } = true;

    // Navigation
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}

public class Incident
{
    public int    Id               { get; set; }
    public string CaseNumber       { get; set; } = string.Empty;   // e.g. RVA-2025-00001
    public string Description      { get; set; } = string.Empty;
    public DateTime ReportedAt     { get; set; } = DateTime.UtcNow;
    public DateTime? OccurredAt    { get; set; }
    public DateTime? UpdatedAt     { get; set; }                    // set by DB trigger
    public bool   IsDeleted        { get; set; }
    public DateTime? DeletedAt     { get; set; }

    // Foreign keys
    public int IncidentTypeId   { get; set; }
    public int IncidentStatusId { get; set; }
    public int LocationId       { get; set; }
    public int OfficerId        { get; set; }

    // Navigation
    public IncidentType   IncidentType   { get; set; } = null!;
    public IncidentStatus IncidentStatus { get; set; } = null!;
    public Location       Location       { get; set; } = null!;
    public Officer        Officer        { get; set; } = null!;
}


