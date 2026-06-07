using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/talentbuilds")]
public class TalentBuildsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TalentBuildsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TalentBuildDto>>> GetAll(string? q, int? playerProfileId, ClassType? classType)
    {
        var query = _db.TalentBuilds
            .AsNoTracking()
            .Include(build => build.PlayerProfile)
            .ThenInclude(player => player!.Guild)
            .Include(build => build.Attachments)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(build =>
                EF.Functions.Like(build.BuildName, pattern) ||
                EF.Functions.Like(build.TalentCode, pattern) ||
                EF.Functions.Like(build.Description, pattern) ||
                (build.PlayerProfile != null && EF.Functions.Like(build.PlayerProfile.CharacterName, pattern)));
        }

        if (playerProfileId.HasValue)
        {
            query = query.Where(build => build.PlayerProfileId == playerProfileId.Value);
        }

        if (classType.HasValue)
        {
            query = query.Where(build => build.PlayerProfile != null && build.PlayerProfile.ClassType == classType.Value);
        }

        var builds = await query
            .OrderByDescending(build => build.PublishedAt)
            .ToListAsync();

        return Ok(builds.Select(build => build.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TalentBuildDto>> GetById(int id)
    {
        var build = await _db.TalentBuilds
            .AsNoTracking()
            .Include(item => item.PlayerProfile)
            .ThenInclude(player => player!.Guild)
            .Include(item => item.Attachments)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (build is null)
        {
            return NotFound();
        }

        return Ok(build.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TalentBuildDto>> Create(TalentBuildUpsertDto dto)
    {
        if (!await _db.PlayerProfiles.AnyAsync(player => player.Id == dto.PlayerProfileId))
        {
            ModelState.AddModelError(nameof(dto.PlayerProfileId), "Selected player does not exist.");
            return ValidationProblem(ModelState);
        }

        var build = new TalentBuild
        {
            BuildName = dto.BuildName.Trim(),
            TalentCode = dto.TalentCode.Trim(),
            Description = dto.Description.Trim(),
            PublishedAt = dto.PublishedAt ?? DateTime.UtcNow,
            PlayerProfileId = dto.PlayerProfileId
        };

        _db.TalentBuilds.Add(build);
        await _db.SaveChangesAsync();

        var created = await _db.TalentBuilds
            .AsNoTracking()
            .Include(item => item.PlayerProfile)
            .ThenInclude(player => player!.Guild)
            .Include(item => item.Attachments)
            .FirstAsync(item => item.Id == build.Id);

        return CreatedAtAction(nameof(GetById), new { id = build.Id }, created.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TalentBuildUpsertDto dto)
    {
        var build = await _db.TalentBuilds.FirstOrDefaultAsync(item => item.Id == id);
        if (build is null)
        {
            return NotFound();
        }

        if (!await _db.PlayerProfiles.AnyAsync(player => player.Id == dto.PlayerProfileId))
        {
            ModelState.AddModelError(nameof(dto.PlayerProfileId), "Selected player does not exist.");
            return ValidationProblem(ModelState);
        }

        build.BuildName = dto.BuildName.Trim();
        build.TalentCode = dto.TalentCode.Trim();
        build.Description = dto.Description.Trim();
        build.PublishedAt = dto.PublishedAt ?? build.PublishedAt;
        build.PlayerProfileId = dto.PlayerProfileId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var build = await _db.TalentBuilds
            .Include(item => item.Attachments)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (build is null)
        {
            return NotFound();
        }

        foreach (var attachment in build.Attachments)
        {
            DeletePhysicalFile(attachment.StoredFilePath);
        }

        _db.TalentBuilds.Remove(build);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void DeletePhysicalFile(string storedFilePath)
    {
        var relativePath = storedFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }
}
