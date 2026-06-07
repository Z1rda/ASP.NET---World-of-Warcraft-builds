using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/playerprofiles")]
public class PlayerProfilesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PlayerProfilesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerProfileDto>>> GetAll(string? q, ClassType? classType, int? guildId, int? minLevel, int? maxLevel, bool? hasGuild)
    {
        var query = _db.PlayerProfiles
            .AsNoTracking()
            .Include(player => player.Guild)
            .Include(player => player.TalentBuilds)
            .Include(player => player.Professions)
            .ThenInclude(link => link.Profession)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(player =>
                EF.Functions.Like(player.CharacterName, pattern) ||
                (player.Guild != null && EF.Functions.Like(player.Guild.Name, pattern)));
        }

        if (classType.HasValue)
        {
            query = query.Where(player => player.ClassType == classType.Value);
        }

        if (guildId.HasValue)
        {
            query = query.Where(player => player.GuildId == guildId.Value);
        }

        if (minLevel.HasValue)
        {
            query = query.Where(player => player.Level >= minLevel.Value);
        }

        if (maxLevel.HasValue)
        {
            query = query.Where(player => player.Level <= maxLevel.Value);
        }

        if (hasGuild.HasValue)
        {
            query = hasGuild.Value ? query.Where(player => player.GuildId != null) : query.Where(player => player.GuildId == null);
        }

        var players = await query
            .OrderBy(player => player.CharacterName)
            .ToListAsync();

        return Ok(players.Select(player => player.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerProfileDto>> GetById(int id)
    {
        var player = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .Include(p => p.TalentBuilds)
            .ThenInclude(build => build.Attachments)
            .Include(p => p.Professions)
            .ThenInclude(link => link.Profession)
            .FirstOrDefaultAsync(player => player.Id == id);

        if (player is null)
        {
            return NotFound();
        }

        return Ok(player.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<PlayerProfileDto>> Create(PlayerProfileUpsertDto dto)
    {
        if (await _db.PlayerProfiles.AnyAsync(player => player.CharacterName.ToLower() == dto.CharacterName.Trim().ToLower()))
        {
            ModelState.AddModelError(nameof(dto.CharacterName), "Player name already exists.");
            return ValidationProblem(ModelState);
        }

        if (dto.GuildId.HasValue && !await _db.Guilds.AnyAsync(guild => guild.Id == dto.GuildId.Value))
        {
            ModelState.AddModelError(nameof(dto.GuildId), "Selected guild does not exist.");
            return ValidationProblem(ModelState);
        }

        var player = new PlayerProfile
        {
            CharacterName = dto.CharacterName.Trim(),
            Level = dto.Level,
            ClassType = dto.ClassType,
            LastUpdatedAt = dto.LastUpdatedAt ?? DateTime.UtcNow,
            GuildId = dto.GuildId
        };

        _db.PlayerProfiles.Add(player);
        await _db.SaveChangesAsync();

        var created = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .Include(p => p.TalentBuilds)
            .Include(p => p.Professions)
            .ThenInclude(link => link.Profession)
            .FirstAsync(p => p.Id == player.Id);

        return CreatedAtAction(nameof(GetById), new { id = player.Id }, created.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PlayerProfileUpsertDto dto)
    {
        var player = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null)
        {
            return NotFound();
        }

        var normalizedName = dto.CharacterName.Trim();
        var duplicate = await _db.PlayerProfiles.AnyAsync(p => p.Id != id && p.CharacterName.ToLower() == normalizedName.ToLower());
        if (duplicate)
        {
            ModelState.AddModelError(nameof(dto.CharacterName), "Player name already exists.");
            return ValidationProblem(ModelState);
        }

        if (dto.GuildId.HasValue && !await _db.Guilds.AnyAsync(guild => guild.Id == dto.GuildId.Value))
        {
            ModelState.AddModelError(nameof(dto.GuildId), "Selected guild does not exist.");
            return ValidationProblem(ModelState);
        }

        player.CharacterName = normalizedName;
        player.Level = dto.Level;
        player.ClassType = dto.ClassType;
        player.LastUpdatedAt = dto.LastUpdatedAt ?? DateTime.UtcNow;
        player.GuildId = dto.GuildId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _db.PlayerProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (player is null)
        {
            return NotFound();
        }

        _db.PlayerProfiles.Remove(player);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
