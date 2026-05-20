using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

public class PlayerProfilesController : Controller
{
    private readonly ApplicationDbContext _db;

    public PlayerProfilesController(ApplicationDbContext db)
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
        return PartialView("_PlayerProfileList", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Suggest(string? q)
    {
        var query = _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .OrderBy(p => p.CharacterName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p =>
                EF.Functions.Like(p.CharacterName, pattern) ||
                (p.Guild != null && EF.Functions.Like(p.Guild.Name, pattern)));
        }

        var results = await query
            .Take(20)
            .Select(p => new { id = p.Id, name = p.CharacterName })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet("/Players/Search")]
    public async Task<IActionResult> Autocomplete(string? q)
    {
        var query = _db.PlayerProfiles
            .AsNoTracking()
            .OrderBy(p => p.CharacterName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p => EF.Functions.Like(p.CharacterName, pattern));
        }

        var results = await query
            .Take(20)
            .Select(p => new { id = p.Id, name = p.CharacterName })
            .ToListAsync();

        return Json(results);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var player = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .Include(p => p.TalentBuilds)
            .Include(p => p.Professions)
            .ThenInclude(pp => pp.Profession)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player is null)
        {
            return NotFound();
        }

        return View(player);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCreateEditSelectionsAsync();
        return View(new PlayerProfile { LastUpdatedAt = DateTime.UtcNow });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlayerProfile player)
    {
        player.CharacterName = (player.CharacterName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulateCreateEditSelectionsAsync(player.GuildId);
            return View(player);
        }

        if (await IsDuplicateNameAsync(player.CharacterName))
        {
            ModelState.AddModelError(nameof(PlayerProfile.CharacterName), "Name already exists");
            await PopulateCreateEditSelectionsAsync(player.GuildId);
            return View(player);
        }

        _db.PlayerProfiles.Add(player);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var player = await _db.PlayerProfiles.FindAsync(id);
        if (player is null)
        {
            return NotFound();
        }

        await PopulateCreateEditSelectionsAsync(player.GuildId);
        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlayerProfile player)
    {
        if (id != player.Id)
        {
            return NotFound();
        }

        player.CharacterName = (player.CharacterName ?? string.Empty).Trim();

        if (!ModelState.IsValid)
        {
            await PopulateCreateEditSelectionsAsync(player.GuildId);
            return View(player);
        }

        if (await IsDuplicateNameAsync(player.CharacterName, player.Id))
        {
            ModelState.AddModelError(nameof(PlayerProfile.CharacterName), "Name already exists");
            await PopulateCreateEditSelectionsAsync(player.GuildId);
            return View(player);
        }

        var existing = await _db.PlayerProfiles.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.CharacterName = player.CharacterName;
        existing.Level = player.Level;
        existing.ClassType = player.ClassType;
        existing.GuildId = player.GuildId;
        existing.LastUpdatedAt = player.LastUpdatedAt;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player is null)
        {
            return NotFound();
        }

        return View(player);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirm = false)
    {
        var player = await _db.PlayerProfiles.FindAsync(id);
        if (player is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _db.PlayerProfiles.Remove(player);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<PlayerProfileIndexViewModel> BuildIndexViewModelAsync(string? q)
    {
        var query = _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .OrderBy(p => p.CharacterName)
            .AsQueryable();

        var trimmed = q?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var pattern = $"%{trimmed}%";
            query = query.Where(p =>
                EF.Functions.Like(p.CharacterName, pattern) ||
                (p.Guild != null && EF.Functions.Like(p.Guild.Name, pattern)));
        }

        var players = await query.ToListAsync();

        return new PlayerProfileIndexViewModel
        {
            Players = players,
            SearchQuery = trimmed ?? string.Empty
        };
    }

    private async Task PopulateCreateEditSelectionsAsync(int? selectedGuildId = null)
    {
        var guilds = await _db.Guilds
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewData["GuildId"] = new SelectList(guilds, "Id", "Name", selectedGuildId);
        ViewData["ClassTypes"] = new SelectList(Enum.GetValues<ClassType>());
    }

    private Task<bool> IsDuplicateNameAsync(string name, int? excludeId = null)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var query = _db.PlayerProfiles
            .AsNoTracking()
            .Where(p => p.CharacterName.ToLower() == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return query.AnyAsync();
    }
}
