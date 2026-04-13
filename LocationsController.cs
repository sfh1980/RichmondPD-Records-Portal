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
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LocationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll([FromQuery] string? precinct = null)
    {
        var query = _db.Locations.AsQueryable();
        if (!string.IsNullOrEmpty(precinct))
            query = query.Where(l => l.Precinct == precinct);
        var locations = await query
            .Select(l => new LocationDto(l.Id, l.Street, l.City, l.State, l.ZipCode, l.Incidents.Count, l.Precinct))
            .ToListAsync();
        return Ok(locations);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        var location = await _db.Locations
            .Where(l => l.Id == id)
            .Select(l => new LocationDto(l.Id, l.Street, l.City, l.State, l.ZipCode, l.Incidents.Count, l.Precinct))
            .FirstOrDefaultAsync();
        if (location is null) return NotFound();
        return Ok(location);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Location location)
    {
        _db.Locations.Add(location);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = location.Id }, location);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Location location)
    {
        var existing = await _db.Locations.FindAsync(id);
        if (existing is null) return NotFound();
        existing.Street = location.Street;
        existing.City = location.City;
        existing.State = location.State;
        existing.ZipCode = location.ZipCode;
        existing.Precinct = location.Precinct;
        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await _db.Locations.FindAsync(id);
        if (location is null) return NotFound();
        _db.Locations.Remove(location);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}