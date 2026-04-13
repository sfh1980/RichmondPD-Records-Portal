using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PolicePortal.Data;
using PolicePortal.DTOs;

namespace PolicePortal.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("open-by-precinct")]
    public async Task<ActionResult<IEnumerable<OpenIncidentByPrecinctResult>>> GetOpenByPrecinct(
        [FromQuery] string? precinct = null)
    {
        var results = new List<OpenIncidentByPrecinctResult>();

        await using var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_GetOpenIncidentsByPrecinct";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Precinct", (object?)precinct ?? DBNull.Value));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new OpenIncidentByPrecinctResult(
                Id: reader.GetInt32(0),
                CaseNumber: reader.GetString(1),
                IncidentType: reader.GetString(2),
                Status: reader.GetString(3),
                Description: reader.GetString(4),
                ReportedAt: reader.GetDateTime(5),
                OccurredAt: reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                OfficerName: reader.GetString(7),
                BadgeNumber: reader.GetString(8),
                Rank: reader.GetString(9),
                Location: reader.GetString(10),
                Precinct: reader.GetString(11)
            ));
        }

        return Ok(results);
    }

    [HttpGet("monthly-summary")]
    public async Task<ActionResult<IEnumerable<MonthlySummaryResult>>> GetMonthlySummary(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        var results = new List<MonthlySummaryResult>();

        await using var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_MonthlyIncidentSummary";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@Year", (object?)year ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@Month", (object?)month ?? DBNull.Value));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new MonthlySummaryResult(
                IncidentType: reader.GetString(0),
                Status: reader.GetString(1),
                Total: reader.GetInt32(2),
                Closed: reader.GetInt32(3),
                Open: reader.GetInt32(4)
            ));
        }

        return Ok(results);
    }

    [HttpGet("officer-workload")]
    public async Task<ActionResult<IEnumerable<OfficerWorkloadResult>>> GetOfficerWorkload(
        [FromQuery] bool activeOnly = true)
    {
        var results = new List<OfficerWorkloadResult>();

        await using var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_OfficerWorkload";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@ActiveOnly", activeOnly));

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new OfficerWorkloadResult(
                BadgeNumber: reader.GetString(0),
                OfficerName: reader.GetString(1),
                Rank: reader.GetString(2),
                Precinct: reader.GetString(3),
                TotalIncidents: reader.GetInt32(4),
                OpenIncidents: reader.GetInt32(5),
                ClosedIncidents: reader.GetInt32(6)
            ));
        }

        return Ok(results);
    }
}
