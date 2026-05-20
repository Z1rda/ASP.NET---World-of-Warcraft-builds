using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

public class TalentBuildsController : Controller
{
    private readonly ApplicationDbContext _db;

    public TalentBuildsController(ApplicationDbContext db)
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
        return PartialView("_TalentBuildList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.TalentBuilds
            .AsNoTracking()
            .Include(t => t.PlayerProfile)
            .OrderBy(t => t.BuildName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(t =>
                EF.Functions.Like(t.BuildName, pattern) ||
                EF.Functions.Like(t.TalentCode, pattern) ||
                (t.PlayerProfile != null && EF.Functions.Like(t.PlayerProfile.CharacterName, pattern)));
        }

        var results = await query
            .Take(20)
            .Select(t => new
            {
                id = t.Id,
                name = t.PlayerProfile == null
                    ? t.BuildName
                    : $"{t.BuildName} - {t.PlayerProfile.CharacterName}"
            })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var build = await _db.TalentBuilds
            .AsNoTracking()
            .Include(t => t.PlayerProfile)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (build is null)
        {
            return NotFound();
        }

        return View(build);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulatePlayerSelectionAsync();
        return View(new TalentBuild { PublishedAt = DateTime.UtcNow });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TalentBuild build)
    {
        build.BuildName = (build.BuildName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulatePlayerSelectionAsync(build.PlayerProfileId);
            return View(build);
        }

        if (await IsDuplicateNameAsync(build.BuildName))
        {
            ModelState.AddModelError(nameof(TalentBuild.BuildName), "Name already exists");
            await PopulatePlayerSelectionAsync(build.PlayerProfileId);
            return View(build);
        }

        _db.TalentBuilds.Add(build);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var build = await _db.TalentBuilds.FindAsync(id);
        if (build is null)
        {
            return NotFound();
        }

        await PopulatePlayerSelectionAsync(build.PlayerProfileId);
        return View(build);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TalentBuild build)
    {
        if (id != build.Id)
        {
            return NotFound();
        }

        build.BuildName = (build.BuildName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulatePlayerSelectionAsync(build.PlayerProfileId);
            return View(build);
        }

        if (await IsDuplicateNameAsync(build.BuildName, build.Id))
        {
            ModelState.AddModelError(nameof(TalentBuild.BuildName), "Name already exists");
            await PopulatePlayerSelectionAsync(build.PlayerProfileId);
            return View(build);
        }

        var existing = await _db.TalentBuilds.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.BuildName = build.BuildName;
        existing.TalentCode = build.TalentCode;
        existing.Description = build.Description;
        existing.PlayerProfileId = build.PlayerProfileId;
        existing.PublishedAt = build.PublishedAt;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsDuplicateNameAsync(string name, int? currentId = null)
    {
        var normalized = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return await _db.TalentBuilds
            .AsNoTracking()
            .AnyAsync(t =>
                t.BuildName.ToLower() == normalized && (!currentId.HasValue || t.Id != currentId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var build = await _db.TalentBuilds
            .AsNoTracking()
            .Include(t => t.PlayerProfile)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (build is null)
        {
            return NotFound();
        }

        return View(build);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var build = await _db.TalentBuilds.FindAsync(id);
        if (build is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.TalentBuilds.Remove(build);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<TalentBuildIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.TalentBuilds
            .AsNoTracking()
            .Include(t => t.PlayerProfile)
            .OrderBy(t => t.BuildName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(t =>
                EF.Functions.Like(t.BuildName, pattern) ||
                EF.Functions.Like(t.TalentCode, pattern) ||
                (t.PlayerProfile != null && EF.Functions.Like(t.PlayerProfile.CharacterName, pattern)));
        }

        var builds = await query.ToListAsync();

        return new TalentBuildIndexViewModel
        {
            TalentBuilds = builds,
            SearchQuery = trimmed ?? string.Empty
        };
    }

    private async Task PopulatePlayerSelectionAsync(int? selectedPlayerId = null)
    {
        var players = await _db.PlayerProfiles
            .AsNoTracking()
            .OrderBy(p => p.CharacterName)
            .ToListAsync();

        ViewData["PlayerProfileId"] = new SelectList(players, "Id", "CharacterName", selectedPlayerId);
    }
}
