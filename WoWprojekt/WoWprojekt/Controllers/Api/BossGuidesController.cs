using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/bossguides")]
public class BossGuidesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BossGuidesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BossGuideDto>>> GetAll(string? q, int? raidGuideId, int? minDifficulty, int? maxDifficulty)
    {
        var query = _db.BossGuides
            .AsNoTracking()
            .Include(boss => boss.RaidGuide)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(boss =>
                EF.Functions.Like(boss.BossName, pattern) ||
                EF.Functions.Like(boss.Tactics, pattern) ||
                (boss.RaidGuide != null && EF.Functions.Like(boss.RaidGuide.RaidName, pattern)));
        }

        if (raidGuideId.HasValue)
        {
            query = query.Where(boss => boss.RaidGuideId == raidGuideId.Value);
        }

        if (minDifficulty.HasValue)
        {
            query = query.Where(boss => boss.DifficultyRating >= minDifficulty.Value);
        }

        if (maxDifficulty.HasValue)
        {
            query = query.Where(boss => boss.DifficultyRating <= maxDifficulty.Value);
        }

        var bosses = await query
            .OrderBy(boss => boss.RaidGuideId)
            .ThenBy(boss => boss.DifficultyRating)
            .ThenBy(boss => boss.BossName)
            .ToListAsync();

        return Ok(bosses.Select(boss => boss.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BossGuideDto>> GetById(int id)
    {
        var boss = await _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (boss is null)
        {
            return NotFound();
        }

        return Ok(boss.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<BossGuideDto>> Create(BossGuideUpsertDto dto)
    {
        if (!await _db.RaidGuides.AnyAsync(raid => raid.Id == dto.RaidGuideId))
        {
            ModelState.AddModelError(nameof(dto.RaidGuideId), "Selected raid guide does not exist.");
            return ValidationProblem(ModelState);
        }

        var boss = new BossGuide
        {
            BossName = dto.BossName.Trim(),
            Tactics = dto.Tactics.Trim(),
            BossImageUrl = dto.BossImageUrl.Trim(),
            DifficultyRating = dto.DifficultyRating,
            RaidGuideId = dto.RaidGuideId
        };

        _db.BossGuides.Add(boss);
        await _db.SaveChangesAsync();

        var created = await _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .FirstAsync(b => b.Id == boss.Id);

        return CreatedAtAction(nameof(GetById), new { id = boss.Id }, created.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BossGuideUpsertDto dto)
    {
        var boss = await _db.BossGuides.FirstOrDefaultAsync(b => b.Id == id);
        if (boss is null)
        {
            return NotFound();
        }

        if (!await _db.RaidGuides.AnyAsync(raid => raid.Id == dto.RaidGuideId))
        {
            ModelState.AddModelError(nameof(dto.RaidGuideId), "Selected raid guide does not exist.");
            return ValidationProblem(ModelState);
        }

        boss.BossName = dto.BossName.Trim();
        boss.Tactics = dto.Tactics.Trim();
        boss.BossImageUrl = dto.BossImageUrl.Trim();
        boss.DifficultyRating = dto.DifficultyRating;
        boss.RaidGuideId = dto.RaidGuideId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var boss = await _db.BossGuides.FirstOrDefaultAsync(b => b.Id == id);
        if (boss is null)
        {
            return NotFound();
        }

        _db.BossGuides.Remove(boss);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
