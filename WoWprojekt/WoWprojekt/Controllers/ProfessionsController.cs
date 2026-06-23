using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

[Route("Professions/{action=Index}/{id?}")]
public class ProfessionsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ProfessionsController(ApplicationDbContext db)
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
        return PartialView("_ProfessionList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.Professions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern));
        }

        var results = await query
            .Take(20)
            .Select(p => new { id = p.Id, name = p.Name })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet("/Professions/Lookup")]
    public async Task<IActionResult> Autocomplete(string? q)
    {
        var query = _db.Professions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern));
        }

        var results = await query
            .Take(20)
            .Select(p => new { id = p.Id, name = p.Name })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var profession = await _db.Professions
            .AsNoTracking()
            .Include(p => p.Players)
            .ThenInclude(pp => pp.PlayerProfile)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profession is null)
        {
            return NotFound();
        }

        return View(profession);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Profession());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Profession profession)
    {
        profession.Name = (profession.Name ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(profession);
        }

        if (await IsDuplicateNameAsync(profession.Name))
        {
            ModelState.AddModelError(nameof(Profession.Name), "Name already exists");
            return View(profession);
        }

        _db.Professions.Add(profession);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var profession = await _db.Professions.FindAsync(id);
        if (profession is null)
        {
            return NotFound();
        }

        return View(profession);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Profession profession)
    {
        if (id != profession.Id)
        {
            return NotFound();
        }

        profession.Name = (profession.Name ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(profession);
        }

        if (await IsDuplicateNameAsync(profession.Name, profession.Id))
        {
            ModelState.AddModelError(nameof(Profession.Name), "Name already exists");
            return View(profession);
        }

        var existing = await _db.Professions.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = profession.Name;
        existing.BenefitDescription = profession.BenefitDescription;

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

        return await _db.Professions
            .AsNoTracking()
            .AnyAsync(p =>
                p.Name.ToLower() == normalized && (!currentId.HasValue || p.Id != currentId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var profession = await _db.Professions
            .AsNoTracking()
            .Include(p => p.Players)
            .ThenInclude(pp => pp.PlayerProfile)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profession is null)
        {
            return NotFound();
        }

        return View(profession);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var profession = await _db.Professions.FindAsync(id);
        if (profession is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.Professions.Remove(profession);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfessionIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.Professions
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p => EF.Functions.Like(p.Name, pattern));
        }

        var professions = await query.ToListAsync();

        return new ProfessionIndexViewModel
        {
            Professions = professions,
            SearchQuery = trimmed ?? string.Empty
        };
    }
}
