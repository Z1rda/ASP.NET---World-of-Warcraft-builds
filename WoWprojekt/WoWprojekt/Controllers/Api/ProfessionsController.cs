using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/professions")]
public class ProfessionsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProfessionsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProfessionDto>>> GetAll(string? q)
    {
        var query = _db.Professions
            .AsNoTracking()
            .Include(p => p.Players)
            .ThenInclude(link => link.PlayerProfile)
            .AsQueryable();

        var trimmedQuery = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            var pattern = $"%{trimmedQuery}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern) || EF.Functions.Like(p.BenefitDescription, pattern));
        }

        var professions = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(professions.Select(profession => profession.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProfessionDto>> GetById(int id)
    {
        var profession = await _db.Professions
            .AsNoTracking()
            .Include(p => p.Players)
            .ThenInclude(link => link.PlayerProfile)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profession is null)
        {
            return NotFound();
        }

        return Ok(profession.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<ProfessionDto>> Create(ProfessionUpsertDto dto)
    {
        var profession = new Profession
        {
            Name = dto.Name.Trim(),
            BenefitDescription = dto.BenefitDescription.Trim()
        };

        if (await _db.Professions.AnyAsync(p => p.Name.ToLower() == profession.Name.ToLower()))
        {
            ModelState.AddModelError(nameof(dto.Name), "Profession name already exists.");
            return ValidationProblem(ModelState);
        }

        _db.Professions.Add(profession);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = profession.Id }, profession.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProfessionUpsertDto dto)
    {
        var profession = await _db.Professions.FirstOrDefaultAsync(p => p.Id == id);
        if (profession is null)
        {
            return NotFound();
        }

        var normalizedName = dto.Name.Trim();
        var duplicate = await _db.Professions.AnyAsync(p => p.Id != id && p.Name.ToLower() == normalizedName.ToLower());
        if (duplicate)
        {
            ModelState.AddModelError(nameof(dto.Name), "Profession name already exists.");
            return ValidationProblem(ModelState);
        }

        profession.Name = normalizedName;
        profession.BenefitDescription = dto.BenefitDescription.Trim();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var profession = await _db.Professions.FirstOrDefaultAsync(p => p.Id == id);
        if (profession is null)
        {
            return NotFound();
        }

        _db.Professions.Remove(profession);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}