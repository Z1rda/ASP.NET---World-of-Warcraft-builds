using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/guilds")]
public class GuildsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public GuildsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GuildDto>>> GetAll(string? q, string? realm, int? minMembers)
    {
        var query = _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(g => EF.Functions.Like(g.Name, pattern) || EF.Functions.Like(g.Realm, pattern));
        }

        var trimmedRealm = realm?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedRealm))
        {
            query = query.Where(g => g.Realm == trimmedRealm);
        }

        if (minMembers.HasValue)
        {
            query = query.Where(g => g.Members.Count >= minMembers.Value);
        }

        var guilds = await query
            .OrderBy(g => g.Name)
            .ToListAsync();

        return Ok(guilds.Select(guild => guild.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuildDto>> GetById(int id)
    {
        var guild = await _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (guild is null)
        {
            return NotFound();
        }

        return Ok(guild.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<GuildDto>> Create(GuildUpsertDto dto)
    {
        var guild = new Guild
        {
            Name = dto.Name.Trim(),
            Realm = dto.Realm.Trim(),
            CreatedAt = dto.CreatedAt ?? DateTime.UtcNow
        };

        if (await _db.Guilds.AnyAsync(g => g.Name.ToLower() == guild.Name.ToLower()))
        {
            ModelState.AddModelError(nameof(dto.Name), "Guild name already exists.");
            return ValidationProblem(ModelState);
        }

        _db.Guilds.Add(guild);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = guild.Id }, guild.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, GuildUpsertDto dto)
    {
        var guild = await _db.Guilds.FirstOrDefaultAsync(g => g.Id == id);
        if (guild is null)
        {
            return NotFound();
        }

        var normalizedName = dto.Name.Trim();
        var duplicate = await _db.Guilds.AnyAsync(g => g.Id != id && g.Name.ToLower() == normalizedName.ToLower());
        if (duplicate)
        {
            ModelState.AddModelError(nameof(dto.Name), "Guild name already exists.");
            return ValidationProblem(ModelState);
        }

        guild.Name = normalizedName;
        guild.Realm = dto.Realm.Trim();
        guild.CreatedAt = dto.CreatedAt ?? guild.CreatedAt;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var guild = await _db.Guilds.FirstOrDefaultAsync(g => g.Id == id);
        if (guild is null)
        {
            return NotFound();
        }

        _db.Guilds.Remove(guild);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}