using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

[Route("BossGuides/{action=Index}/{id?}")]
public class BossGuidesController : Controller
{
    private readonly ApplicationDbContext _db;

    public BossGuidesController(ApplicationDbContext db)
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
        return PartialView("_BossGuideList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .OrderBy(b => b.BossName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(b =>
                EF.Functions.Like(b.BossName, pattern) ||
                (b.RaidGuide != null && EF.Functions.Like(b.RaidGuide.RaidName, pattern)));
        }

        var results = await query
            .Take(20)
            .Select(b => new { id = b.Id, name = b.BossName })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var boss = await _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (boss is null)
        {
            return NotFound();
        }

        return View(boss);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateRaidSelectionAsync();
        return View(new BossGuide());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BossGuide boss)
    {
        boss.BossName = (boss.BossName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulateRaidSelectionAsync(boss.RaidGuideId);
            return View(boss);
        }

        if (await IsDuplicateNameAsync(boss.BossName))
        {
            ModelState.AddModelError(nameof(BossGuide.BossName), "Name already exists");
            await PopulateRaidSelectionAsync(boss.RaidGuideId);
            return View(boss);
        }

        _db.BossGuides.Add(boss);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var boss = await _db.BossGuides.FindAsync(id);
        if (boss is null)
        {
            return NotFound();
        }

        await PopulateRaidSelectionAsync(boss.RaidGuideId);
        return View(boss);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BossGuide boss)
    {
        if (id != boss.Id)
        {
            return NotFound();
        }

        boss.BossName = (boss.BossName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulateRaidSelectionAsync(boss.RaidGuideId);
            return View(boss);
        }

        if (await IsDuplicateNameAsync(boss.BossName, boss.Id))
        {
            ModelState.AddModelError(nameof(BossGuide.BossName), "Name already exists");
            await PopulateRaidSelectionAsync(boss.RaidGuideId);
            return View(boss);
        }

        var existing = await _db.BossGuides.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.BossName = boss.BossName;
        existing.Tactics = boss.Tactics;
        existing.BossImageUrl = boss.BossImageUrl;
        existing.DifficultyRating = boss.DifficultyRating;
        existing.RaidGuideId = boss.RaidGuideId;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsDuplicateNameAsync(string bossName, int? currentId = null)
    {
        var normalized = bossName.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return await _db.BossGuides
            .AsNoTracking()
            .AnyAsync(b =>
                b.BossName.ToLower() == normalized && (!currentId.HasValue || b.Id != currentId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var boss = await _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (boss is null)
        {
            return NotFound();
        }

        return View(boss);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var boss = await _db.BossGuides.FindAsync(id);
        if (boss is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.BossGuides.Remove(boss);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<BossGuideIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .OrderBy(b => b.BossName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(b =>
                EF.Functions.Like(b.BossName, pattern) ||
                (b.RaidGuide != null && EF.Functions.Like(b.RaidGuide.RaidName, pattern)));
        }

        var bosses = await query.ToListAsync();

        return new BossGuideIndexViewModel
        {
            Bosses = bosses,
            SearchQuery = trimmed ?? string.Empty
        };
    }

    private async Task PopulateRaidSelectionAsync(int? selectedRaidId = null)
    {
        var raids = await _db.RaidGuides
            .AsNoTracking()
            .OrderBy(r => r.RaidName)
            .ToListAsync();

        ViewData["RaidGuideId"] = new SelectList(raids, "Id", "RaidName", selectedRaidId);
    }
}
