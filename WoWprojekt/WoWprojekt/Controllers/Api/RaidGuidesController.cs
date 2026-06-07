using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/raidguides")]
public class RaidGuidesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RaidGuidesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RaidGuideDto>>> GetAll(string? q, int? minBossCount)
    {
        var query = _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(r => EF.Functions.Like(r.RaidName, pattern) || EF.Functions.Like(r.PreparationNotes, pattern));
        }

        if (minBossCount.HasValue)
        {
            query = query.Where(r => r.Bosses.Count >= minBossCount.Value);
        }

        var raids = await query
            .OrderBy(r => r.RaidName)
            .ToListAsync();

        return Ok(raids.Select(raid => raid.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RaidGuideDto>> GetById(int id)
    {
        var raid = await _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (raid is null)
        {
            return NotFound();
        }

        return Ok(raid.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<RaidGuideDto>> Create(RaidGuideUpsertDto dto)
    {
        var raid = new RaidGuide
        {
            RaidName = dto.RaidName.Trim(),
            PreparationNotes = dto.PreparationNotes.Trim(),
            UpdatedAt = dto.UpdatedAt ?? DateTime.UtcNow
        };

        if (await _db.RaidGuides.AnyAsync(r => r.RaidName.ToLower() == raid.RaidName.ToLower()))
        {
            ModelState.AddModelError(nameof(dto.RaidName), "Raid name already exists.");
            return ValidationProblem(ModelState);
        }

        _db.RaidGuides.Add(raid);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = raid.Id }, raid.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RaidGuideUpsertDto dto)
    {
        var raid = await _db.RaidGuides.FirstOrDefaultAsync(r => r.Id == id);
        if (raid is null)
        {
            return NotFound();
        }

        var normalizedName = dto.RaidName.Trim();
        var duplicate = await _db.RaidGuides.AnyAsync(r => r.Id != id && r.RaidName.ToLower() == normalizedName.ToLower());
        if (duplicate)
        {
            ModelState.AddModelError(nameof(dto.RaidName), "Raid name already exists.");
            return ValidationProblem(ModelState);
        }

        raid.RaidName = normalizedName;
        raid.PreparationNotes = dto.PreparationNotes.Trim();
        raid.UpdatedAt = dto.UpdatedAt ?? raid.UpdatedAt;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var raid = await _db.RaidGuides.FirstOrDefaultAsync(r => r.Id == id);
        if (raid is null)
        {
            return NotFound();
        }

        _db.RaidGuides.Remove(raid);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}