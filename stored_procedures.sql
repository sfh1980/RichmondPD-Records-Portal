-- =============================================================================
-- PolicePortal Database Scripts
-- Run AFTER EF Core migration has created the base schema.
-- =============================================================================

USE PolicePortal;
GO

-- =============================================================================
-- TRIGGER: Auto-set UpdatedAt on Incidents when a row is modified
-- =============================================================================
CREATE OR ALTER TRIGGER trg_Incidents_UpdatedAt
ON Incidents
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Incidents
    SET UpdatedAt = GETUTCDATE()
    FROM Incidents i
    INNER JOIN inserted ins ON i.Id = ins.Id;
END;
GO

-- =============================================================================
-- STORED PROCEDURE: Get open incidents by precinct with officer info
-- =============================================================================
CREATE OR ALTER PROCEDURE sp_GetOpenIncidentsByPrecinct
    @Precinct NVARCHAR(50) = NULL   -- NULL = all precincts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.Id,
        i.CaseNumber,
        it.[Name]                           AS IncidentType,
        ist.[Name]                          AS Status,
        i.Description,
        i.ReportedAt,
        i.OccurredAt,
        o.FirstName + ' ' + o.LastName     AS OfficerName,
        o.BadgeNumber,
        o.[Rank],
        l.Street + ', ' + l.City           AS Location,
        l.Precinct
    FROM Incidents i
    INNER JOIN IncidentStatuses ist ON i.IncidentStatusId = ist.Id
    INNER JOIN IncidentTypes    it  ON i.IncidentTypeId   = it.Id
    INNER JOIN Officers          o  ON i.OfficerId        = o.Id
    INNER JOIN Locations         l  ON i.LocationId       = l.Id
    WHERE
        ist.[Name] = 'Open'
        AND i.IsDeleted = 0
        AND (@Precinct IS NULL OR l.Precinct = @Precinct)
    ORDER BY i.ReportedAt DESC;
END;
GO

-- =============================================================================
-- STORED PROCEDURE: Monthly incident summary report
-- =============================================================================
CREATE OR ALTER PROCEDURE sp_MonthlyIncidentSummary
    @Year  INT = NULL,
    @Month INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Year  = ISNULL(@Year,  YEAR(GETUTCDATE()));
    SET @Month = ISNULL(@Month, MONTH(GETUTCDATE()));

    SELECT
        it.[Name]           AS IncidentType,
        ist.[Name]          AS [Status],
        COUNT(*)            AS Total,
        COUNT(CASE WHEN ist.[Name] = 'Closed' THEN 1 END) AS Closed,
        COUNT(CASE WHEN ist.[Name] = 'Open'   THEN 1 END) AS [Open]
    FROM Incidents i
    INNER JOIN IncidentTypes    it  ON i.IncidentTypeId   = it.Id
    INNER JOIN IncidentStatuses ist ON i.IncidentStatusId = ist.Id
    WHERE
        i.IsDeleted = 0
        AND
        YEAR(i.ReportedAt)  = @Year
        AND MONTH(i.ReportedAt) = @Month
    GROUP BY it.[Name], ist.[Name]
    ORDER BY Total DESC;
END;
GO

-- =============================================================================
-- STORED PROCEDURE: Officer workload (incident count per officer)
-- =============================================================================
CREATE OR ALTER PROCEDURE sp_OfficerWorkload
    @ActiveOnly BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.BadgeNumber,
        o.FirstName + ' ' + o.LastName  AS OfficerName,
        o.[Rank],
        o.Precinct,
        COUNT(i.Id)                     AS TotalIncidents,
        COUNT(CASE WHEN ist.[Name] = 'Open' THEN 1 END)  AS OpenIncidents,
        COUNT(CASE WHEN ist.[Name] = 'Closed' THEN 1 END) AS ClosedIncidents
    FROM Officers o
    LEFT JOIN Incidents i        ON o.Id = i.OfficerId AND i.IsDeleted = 0
    LEFT JOIN IncidentStatuses ist ON i.IncidentStatusId = ist.Id
    WHERE (@ActiveOnly = 0 OR o.IsActive = 1)
    GROUP BY o.Id, o.BadgeNumber, o.FirstName, o.LastName, o.[Rank], o.Precinct
    ORDER BY TotalIncidents DESC;
END;
GO

-- =============================================================================
-- INDEXES for performance on common query patterns
-- =============================================================================

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Filter by status (most common dashboard query)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Incidents_StatusId'
      AND object_id = OBJECT_ID('dbo.Incidents')
)
BEGIN
    CREATE INDEX IX_Incidents_StatusId
        ON Incidents (IncidentStatusId)
        INCLUDE (CaseNumber, ReportedAt, OfficerId);
END;

-- Filter by date range
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Incidents_ReportedAt'
      AND object_id = OBJECT_ID('dbo.Incidents')
)
BEGIN
    CREATE INDEX IX_Incidents_ReportedAt
        ON Incidents (ReportedAt DESC);
END;

-- Officer lookup
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Officers_Precinct'
      AND object_id = OBJECT_ID('dbo.Officers')
)
BEGIN
    CREATE INDEX IX_Officers_Precinct
        ON Officers (Precinct)
        WHERE IsActive = 1;
END;

GO
PRINT 'All stored procedures, triggers, and indexes created successfully.';
