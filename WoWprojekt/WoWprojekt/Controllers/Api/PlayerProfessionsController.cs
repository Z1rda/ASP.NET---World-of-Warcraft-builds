using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/playerprofessions")]
public class PlayerProfessionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PlayerProfessionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerProfessionDto>>> GetAll(int? playerProfileId, int? professionId, int? minSkillLevel, int? maxSkillLevel, string? q)
    {
        var query = _db.PlayerProfessions
            .AsNoTracking()
            .Include(link => link.PlayerProfile)
            .Include(link => link.Profession)
            .AsQueryable();

        if (playerProfileId.HasValue)
        {
            query = query.Where(link => link.PlayerProfileId == playerProfileId.Value);
        }

        if (professionId.HasValue)
        {
            query = query.Where(link => link.ProfessionId == professionId.Value);
        }

        if (minSkillLevel.HasValue)
        {
            query = query.Where(link => link.SkillLevel >= minSkillLevel.Value);
        }

        if (maxSkillLevel.HasValue)
        {
            query = query.Where(link => link.SkillLevel <= maxSkillLevel.Value);
        }

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(link =>
                (link.PlayerProfile != null && EF.Functions.Like(link.PlayerProfile.CharacterName, pattern)) ||
                (link.Profession != null && EF.Functions.Like(link.Profession.Name, pattern)));
        }

        var links = await query
            .OrderBy(link => link.PlayerProfileId)
            .ThenBy(link => link.ProfessionId)
            .ToListAsync();

        return Ok(links.Select(link => link.ToDto()));
    }

    [HttpGet("{playerProfileId:int}/{professionId:int}")]
    public async Task<ActionResult<PlayerProfessionDto>> GetById(int playerProfileId, int professionId)
    {
        var link = await _db.PlayerProfessions
            .AsNoTracking()
            .Include(item => item.PlayerProfile)
            .Include(item => item.Profession)
            .FirstOrDefaultAsync(item => item.PlayerProfileId == playerProfileId && item.ProfessionId == professionId);

        if (link is null)
        {
            return NotFound();
        }

        return Ok(link.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<PlayerProfessionDto>> Create(PlayerProfessionUpsertDto dto)
    {
        if (!await _db.PlayerProfiles.AnyAsync(player => player.Id == dto.PlayerProfileId))
        {
            ModelState.AddModelError(nameof(dto.PlayerProfileId), "Selected player does not exist.");
            return ValidationProblem(ModelState);
        }

        if (!await _db.Professions.AnyAsync(profession => profession.Id == dto.ProfessionId))
        {
            ModelState.AddModelError(nameof(dto.ProfessionId), "Selected profession does not exist.");
            return ValidationProblem(ModelState);
        }

        var exists = await _db.PlayerProfessions.AnyAsync(link => link.PlayerProfileId == dto.PlayerProfileId && link.ProfessionId == dto.ProfessionId);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "That player-profession link already exists.");
            return ValidationProblem(ModelState);
        }

        var link = new PlayerProfession
        {
            PlayerProfileId = dto.PlayerProfileId,
            ProfessionId = dto.ProfessionId,
            SkillLevel = dto.SkillLevel
        };

        _db.PlayerProfessions.Add(link);
        await _db.SaveChangesAsync();

        var created = await _db.PlayerProfessions
            .AsNoTracking()
            .Include(item => item.PlayerProfile)
            .Include(item => item.Profession)
            .FirstAsync(item => item.PlayerProfileId == link.PlayerProfileId && item.ProfessionId == link.ProfessionId);

        return CreatedAtAction(nameof(GetById), new { playerProfileId = link.PlayerProfileId, professionId = link.ProfessionId }, created.ToDto());
    }

    [HttpPut("{playerProfileId:int}/{professionId:int}")]
    public async Task<IActionResult> Update(int playerProfileId, int professionId, PlayerProfessionUpsertDto dto)
    {
        if (playerProfileId != dto.PlayerProfileId || professionId != dto.ProfessionId)
        {
            return BadRequest("Route keys must match the request body.");
        }

        var link = await _db.PlayerProfessions.FirstOrDefaultAsync(item => item.PlayerProfileId == playerProfileId && item.ProfessionId == professionId);
        if (link is null)
        {
            return NotFound();
        }

        link.SkillLevel = dto.SkillLevel;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{playerProfileId:int}/{professionId:int}")]
    public async Task<IActionResult> Delete(int playerProfileId, int professionId)
    {
        var link = await _db.PlayerProfessions.FirstOrDefaultAsync(item => item.PlayerProfileId == playerProfileId && item.ProfessionId == professionId);
        if (link is null)
        {
            return NotFound();
        }

        _db.PlayerProfessions.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
