using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolicePortal.Data;
using PolicePortal.DTOs;
using PolicePortal.Models;

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
        var query = _db.Officers.Include(o => o.Incidents).AsQueryable();
        if (activeOnly.HasValue)
            query = query.Where(o => o.IsActive == activeOnly.Value);

        var officers = await query
            .OrderBy(o => o.LastName)
            .Select(o => new OfficerDto(
                o.Id, o.BadgeNumber, o.FirstName, o.LastName,
                o.Rank, o.Precinct, o.IsActive, o.Incidents.Count))
            .ToListAsync();

        return Ok(officers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OfficerDto>> GetById(int id)
    {
        var o = await _db.Officers.Include(x => x.Incidents).FirstOrDefaultAsync(x => x.Id == id);
        if (o is null) return NotFound();
        return Ok(new OfficerDto(o.Id, o.BadgeNumber, o.FirstName, o.LastName,
            o.Rank, o.Precinct, o.IsActive, o.Incidents.Count));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Officer officer)
    {
        _db.Officers.Add(officer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = officer.Id }, officer);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Officer updated)
    {
        var officer = await _db.Officers.FindAsync(id);
        if (officer is null) return NotFound();

        officer.FirstName   = updated.FirstName;
        officer.LastName    = updated.LastName;
        officer.Rank        = updated.Rank;
        officer.Precinct    = updated.Precinct;
        officer.IsActive    = updated.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
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
}
