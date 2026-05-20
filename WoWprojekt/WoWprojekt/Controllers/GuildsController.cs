using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

public class GuildsController : Controller
{
    private readonly ApplicationDbContext _db;

    public GuildsController(ApplicationDbContext db)
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
        return PartialView("_GuildList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.Guilds
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(g =>
                EF.Functions.Like(g.Name, pattern) ||
                EF.Functions.Like(g.Realm, pattern));
        }

        var results = await query
            .Take(20)
            .Select(g => new
            {
                id = g.Id,
                name = string.IsNullOrWhiteSpace(g.Realm) ? g.Name : $"{g.Name} - {g.Realm}"
            })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var guild = await _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (guild is null)
        {
            return NotFound();
        }

        return View(guild);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Guild { CreatedAt = DateTime.UtcNow });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guild guild)
    {
        guild.Name = (guild.Name ?? string.Empty).Trim();
        guild.Realm = (guild.Realm ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(guild);
        }

        if (await IsDuplicateNameAsync(guild.Name))
        {
            ModelState.AddModelError(nameof(Guild.Name), "Name already exists");
            return View(guild);
        }

        guild.CreatedAt = DateTime.UtcNow;
        _db.Guilds.Add(guild);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var guild = await _db.Guilds.FindAsync(id);
        if (guild is null)
        {
            return NotFound();
        }

        return View(guild);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Guild guild)
    {
        if (id != guild.Id)
        {
            return NotFound();
        }

        guild.Name = (guild.Name ?? string.Empty).Trim();
        guild.Realm = (guild.Realm ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            return View(guild);
        }

        if (await IsDuplicateNameAsync(guild.Name, guild.Id))
        {
            ModelState.AddModelError(nameof(Guild.Name), "Name already exists");
            return View(guild);
        }

        var existing = await _db.Guilds.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = guild.Name;
        existing.Realm = guild.Realm;

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

        return await _db.Guilds
            .AsNoTracking()
            .AnyAsync(g =>
                g.Name.ToLower() == normalized && (!currentId.HasValue || g.Id != currentId.Value));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var guild = await _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (guild is null)
        {
            return NotFound();
        }

        return View(guild);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var guild = await _db.Guilds.FindAsync(id);
        if (guild is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.Guilds.Remove(guild);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<GuildIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .OrderBy(g => g.Name)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(g =>
                EF.Functions.Like(g.Name, pattern) ||
                EF.Functions.Like(g.Realm, pattern));
        }

        var guilds = await query.ToListAsync();

        return new GuildIndexViewModel
        {
            Guilds = guilds,
            SearchQuery = trimmed ?? string.Empty
        };
    }
}
