using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

[Route("PlayerProfessions/{action=Index}/{id?}")]
public class PlayerProfessionsController : Controller
{
    private readonly ApplicationDbContext _db;

    public PlayerProfessionsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var vm = await BuildIndexViewModelAsync(q);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? q)
    {
        var vm = await BuildIndexViewModelAsync(q);
        return PartialView("_PlayerProfessionList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.PlayerProfessions
            .AsNoTracking()
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .OrderBy(pp => pp.PlayerProfileId)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(pp =>
                (pp.PlayerProfile != null && EF.Functions.Like(pp.PlayerProfile.CharacterName, pattern)) ||
                (pp.Profession != null && EF.Functions.Like(pp.Profession.Name, pattern)));
        }

        var results = await query
            .Take(20)
            .Select(pp => new
            {
                id = $"{pp.PlayerProfileId}-{pp.ProfessionId}",
                name = pp.PlayerProfile == null || pp.Profession == null
                    ? "Unknown"
                    : $"{pp.PlayerProfile.CharacterName} - {pp.Profession.Name}"
            })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int playerId, int professionId)
    {
        var link = await _db.PlayerProfessions
            .AsNoTracking()
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .FirstOrDefaultAsync(pp => pp.PlayerProfileId == playerId && pp.ProfessionId == professionId);

        if (link is null)
        {
            return NotFound();
        }

        return View(link);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateSelectionsAsync();
        return View(new PlayerProfession());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlayerProfession link)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(link.PlayerProfileId, link.ProfessionId);
            return View(link);
        }

        var exists = await _db.PlayerProfessions
            .AnyAsync(pp => pp.PlayerProfileId == link.PlayerProfileId && pp.ProfessionId == link.ProfessionId);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "This player already has the selected profession.");
            await PopulateSelectionsAsync(link.PlayerProfileId, link.ProfessionId);
            return View(link);
        }

        _db.PlayerProfessions.Add(link);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int playerId, int professionId)
    {
        var link = await _db.PlayerProfessions
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .FirstOrDefaultAsync(pp => pp.PlayerProfileId == playerId && pp.ProfessionId == professionId);

        if (link is null)
        {
            return NotFound();
        }

        return View(link);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int playerId, int professionId, PlayerProfession link)
    {
        if (playerId != link.PlayerProfileId || professionId != link.ProfessionId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(link);
        }

        var existing = await _db.PlayerProfessions
            .FirstOrDefaultAsync(pp => pp.PlayerProfileId == playerId && pp.ProfessionId == professionId);

        if (existing is null)
        {
            return NotFound();
        }

        existing.SkillLevel = link.SkillLevel;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int playerId, int professionId)
    {
        var link = await _db.PlayerProfessions
            .AsNoTracking()
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .FirstOrDefaultAsync(pp => pp.PlayerProfileId == playerId && pp.ProfessionId == professionId);

        if (link is null)
        {
            return NotFound();
        }

        return View(link);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int playerId, int professionId, bool confirm = false)
    {
        var link = await _db.PlayerProfessions
            .FirstOrDefaultAsync(pp => pp.PlayerProfileId == playerId && pp.ProfessionId == professionId);

        if (link is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.PlayerProfessions.Remove(link);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<PlayerProfessionIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.PlayerProfessions
            .AsNoTracking()
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .OrderBy(pp => pp.PlayerProfileId)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(pp =>
                (pp.PlayerProfile != null && EF.Functions.Like(pp.PlayerProfile.CharacterName, pattern)) ||
                (pp.Profession != null && EF.Functions.Like(pp.Profession.Name, pattern)));
        }

        var links = await query.ToListAsync();

        return new PlayerProfessionIndexViewModel
        {
            PlayerProfessions = links,
            SearchQuery = trimmed ?? string.Empty
        };
    }

    private async Task PopulateSelectionsAsync(int? selectedPlayerId = null, int? selectedProfessionId = null)
    {
        var players = await _db.PlayerProfiles
            .AsNoTracking()
            .OrderBy(p => p.CharacterName)
            .ToListAsync();

        var professions = await _db.Professions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewData["PlayerProfileId"] = new SelectList(players, "Id", "CharacterName", selectedPlayerId);
        ViewData["ProfessionId"] = new SelectList(professions, "Id", "Name", selectedProfessionId);
    }
}
