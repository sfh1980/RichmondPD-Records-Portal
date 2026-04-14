using System.ComponentModel.DataAnnotations;

namespace PolicePortal.DTOs;

// ─── Incident ──────────────────────────────────────────────────────────────────

public record IncidentDto(
    int       Id,
    string    CaseNumber,
    string    Description,
    DateTime  ReportedAt,
    DateTime? OccurredAt,
    DateTime? UpdatedAt,
    int       IncidentTypeId,
    int       IncidentStatusId,
    int       LocationId,
    int       OfficerId,
    string    IncidentType,
    string    IncidentStatus,
    string    StatusColor,
    string    OfficerName,
    string    BadgeNumber,
    string    LocationAddress,
    string?   Precinct,
    bool      IsDeleted,
    DateTime? DeletedAt
);

public record CreateIncidentRequest(
    [Required, StringLength(2000, MinimumLength = 5)]
    string    Description,
    DateTime? OccurredAt,
    [Range(1, int.MaxValue)]
    int       IncidentTypeId,
    [Range(1, int.MaxValue)]
    int       IncidentStatusId,
    [Range(1, int.MaxValue)]
    int       LocationId,
    [Range(1, int.MaxValue)]
    int       OfficerId
);

public record UpdateIncidentRequest(
    [Required, StringLength(2000, MinimumLength = 5)]
    string    Description,
    DateTime? OccurredAt,
    [Range(1, int.MaxValue)]
    int       IncidentTypeId,
    [Range(1, int.MaxValue)]
    int       IncidentStatusId,
    [Range(1, int.MaxValue)]
    int       LocationId,
    [Range(1, int.MaxValue)]
    int       OfficerId
);

// ─── Officer ───────────────────────────────────────────────────────────────────

public record OfficerDto(
    int    Id,
    string BadgeNumber,
    string FirstName,
    string LastName,
    string Rank,
    string Precinct,
    bool   IsActive,
    int    IncidentCount
);

public record CreateOfficerRequest(
    [Required, StringLength(20, MinimumLength = 3)]
    string BadgeNumber,
    [Required, StringLength(100)]
    string FirstName,
    [Required, StringLength(100)]
    string LastName,
    [Required, StringLength(100)]
    string Rank,
    [Required, StringLength(50)]
    string Precinct,
    bool IsActive = true
);

public record UpdateOfficerRequest(
    [Required, StringLength(20, MinimumLength = 3)]
    string BadgeNumber,
    [Required, StringLength(100)]
    string FirstName,
    [Required, StringLength(100)]
    string LastName,
    [Required, StringLength(100)]
    string Rank,
    [Required, StringLength(50)]
    string Precinct,
    bool IsActive
);

// ─── Location ──────────────────────────────────────────────────────────────────

public record LocationDto(
    int    Id,
    string Street,
    string City,
    string State,
    string ZipCode,
    int IncidentCount,
    string Precinct
    
);

// ─── Reports (stored procedure results) ──────────────────────────────────────

public record OpenIncidentByPrecinctResult(
    int Id,
    string CaseNumber,
    string IncidentType,
    string Status,
    string Description,
    DateTime ReportedAt,
    DateTime? OccurredAt,
    string OfficerName,
    string BadgeNumber,
    string Rank,
    string Location,
    string Precinct
);

public record MonthlySummaryResult(
    string IncidentType,
    string Status,
    int Total,
    int Closed,
    int Open
);

public record OfficerWorkloadResult(
    string BadgeNumber,
    string OfficerName,
    string Rank,
    string Precinct,
    int TotalIncidents,
    int OpenIncidents,
    int ClosedIncidents
);

// ─── Auth ──────────────────────────────────────────────────────────────────────

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, DateTime Expires);

// ─── Stats (for dashboard) ─────────────────────────────────────────────────────

public record DashboardStats(
    int TotalIncidents,
    int OpenIncidents,
    int ClosedThisMonth,
    int UnderInvestigation,
    IEnumerable<IncidentsByType>   ByType,
    IEnumerable<IncidentsByStatus> ByStatus
);

public record IncidentsByType(string Type, int Count);
public record IncidentsByStatus(string Status, string Color, int Count);
