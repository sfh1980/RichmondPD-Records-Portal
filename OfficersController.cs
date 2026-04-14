using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicePortal.Data;
using PolicePortal.DTOs;

namespace PolicePortal.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OfficersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OfficersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OfficerDto>>> GetAll([FromQuery] bool? activeOnly = null)
    {
        var query = _db.Officers.AsQueryable();
        if (activeOnly.HasValue)
            query = query.Where(o => o.IsActive == activeOnly.Value);

        var officers = await query
            .OrderBy(o => o.LastName)
            .Select(MapToDto())
            .ToListAsync();

        return Ok(officers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OfficerDto>> GetById(int id)
    {
        var officer = await _db.Officers
            .Where(o => o.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync();

        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost]
    public async Task<ActionResult<OfficerDto>> Create([FromBody] CreateOfficerRequest request)
    {
        var officer = new PolicePortal.Models.Officer
        {
            BadgeNumber = request.BadgeNumber.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Rank = request.Rank.Trim(),
            Precinct = request.Precinct.Trim(),
            IsActive = request.IsActive
        };

        _db.Officers.Add(officer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = officer.Id }, await LoadOfficerDto(officer.Id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OfficerDto>> Update(int id, [FromBody] UpdateOfficerRequest request)
    {
        var officer = await _db.Officers.FindAsync(id);
        if (officer is null) return NotFound();

        officer.BadgeNumber = request.BadgeNumber.Trim();
        officer.FirstName   = request.FirstName.Trim();
        officer.LastName    = request.LastName.Trim();
        officer.Rank        = request.Rank.Trim();
        officer.Precinct    = request.Precinct.Trim();
        officer.IsActive    = request.IsActive;

        await _db.SaveChangesAsync();
        return Ok(await LoadOfficerDto(id));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var officer = await _db.Officers.FindAsync(id);
        if (officer is null) return NotFound();
        officer.IsActive = false; // soft delete
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<ActionResult<OfficerDto>> Reactivate(int id)
    {
        var officer = await _db.Officers.FindAsync(id);
        if (officer is null) return NotFound();

        officer.IsActive = true;
        await _db.SaveChangesAsync();
        return Ok(await LoadOfficerDto(id));
    }

    private async Task<OfficerDto> LoadOfficerDto(int id) =>
        await _db.Officers
            .Where(o => o.Id == id)
            .Select(MapToDto())
            .FirstAsync();

    private static System.Linq.Expressions.Expression<Func<PolicePortal.Models.Officer, OfficerDto>> MapToDto() =>
        officer => new OfficerDto(
            officer.Id,
            officer.BadgeNumber,
            officer.FirstName,
            officer.LastName,
            officer.Rank,
            officer.Precinct,
            officer.IsActive,
            officer.Incidents.Count(i => !i.IsDeleted));
}
