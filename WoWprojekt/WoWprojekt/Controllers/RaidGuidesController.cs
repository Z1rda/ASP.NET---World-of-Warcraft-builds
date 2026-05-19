using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

public class RaidGuidesController : Controller
{
    private readonly ApplicationDbContext _db;

    public RaidGuidesController(ApplicationDbContext db)
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
        return PartialView("_RaidGuideList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.RaidGuides
            .AsNoTracking()
            .OrderBy(r => r.RaidName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(r => EF.Functions.Like(r.RaidName, pattern));
        }

        var results = await query
            .Take(20)
            .Select(r => new { id = r.Id, name = r.RaidName })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet("/Raids/Search")]
    public async Task<IActionResult> Autocomplete(string? q)
    {
        var query = _db.RaidGuides
            .AsNoTracking()
            .OrderBy(r => r.RaidName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(r => EF.Functions.Like(r.RaidName, pattern));
        }

        var results = await query
            .Take(20)
            .Select(r => new { id = r.Id, name = r.RaidName })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var raid = await _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (raid is null)
        {
            return NotFound();
        }

        return View(raid);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new RaidGuide { UpdatedAt = DateTime.UtcNow });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RaidGuide raid)
    {
        if (!ModelState.IsValid)
        {
            return View(raid);
        }

        raid.UpdatedAt = DateTime.UtcNow;
        _db.RaidGuides.Add(raid);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var raid = await _db.RaidGuides.FindAsync(id);
        if (raid is null)
        {
            return NotFound();
        }

        return View(raid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RaidGuide raid)
    {
        if (id != raid.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(raid);
        }

        var existing = await _db.RaidGuides.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.RaidName = raid.RaidName;
        existing.PreparationNotes = raid.PreparationNotes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var raid = await _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (raid is null)
        {
            return NotFound();
        }

        return View(raid);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var raid = await _db.RaidGuides.FindAsync(id);
        if (raid is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.RaidGuides.Remove(raid);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<RaidGuideIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .OrderBy(r => r.RaidName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(r => EF.Functions.Like(r.RaidName, pattern));
        }

        var raids = await query.ToListAsync();

        return new RaidGuideIndexViewModel
        {
            Raids = raids,
            SearchQuery = trimmed ?? string.Empty
        };
    }
}
