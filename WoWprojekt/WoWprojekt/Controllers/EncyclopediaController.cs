using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Data;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;

namespace WoWprojekt.Controllers;

public class EncyclopediaController : Controller
{
    private readonly ApplicationDbContext _db;

    public EncyclopediaController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("directory/players")]
    public async Task<IActionResult> Players(int? id)
    {
        var players = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Guild)
            .Include(p => p.TalentBuilds)
            .Include(p => p.Professions)
            .ThenInclude(pp => pp.Profession)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? players.FirstOrDefault(p => p.Id == id.Value)
            : players.FirstOrDefault();

        var vm = new PlayerDirectoryPageViewModel
        {
            Players = players,
            SelectedPlayer = selected
        };

        return View(vm);
    }

    [HttpGet("directory/raids")]
    public async Task<IActionResult> Raids(int? id, List<int>? selectedBossIds, bool filterApplied = false)
    {
        var raids = await _db.RaidGuides
            .AsNoTracking()
            .Include(r => r.Bosses)
            .OrderBy(r => r.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? raids.FirstOrDefault(r => r.Id == id.Value)
            : raids.FirstOrDefault();

        var raidBosses = selected?.Bosses.ToList() ?? new List<BossGuide>();
        var raidBossIdSet = raidBosses.Select(b => b.Id).ToHashSet();

        var sanitizedSelectedIds = (selectedBossIds ?? new List<int>())
            .Where(raidBossIdSet.Contains)
            .ToHashSet();

        if (!filterApplied)
        {
            sanitizedSelectedIds = raidBosses.Select(b => b.Id).ToHashSet();
        }

        var visibleBosses = raidBosses
            .Where(b => sanitizedSelectedIds.Contains(b.Id))
            .ToList();

        var vm = new RaidDirectoryPageViewModel
        {
            Raids = raids,
            SelectedRaid = selected,
            VisibleBosses = visibleBosses,
            SelectedBossIds = sanitizedSelectedIds,
            FilterApplied = filterApplied
        };

        return View(vm);
    }

    [HttpGet("directory/professions")]
    public async Task<IActionResult> Professions(int? id)
    {
        var professions = await _db.Professions
            .AsNoTracking()
            .Include(p => p.Players)
            .ThenInclude(pp => pp.PlayerProfile)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var players = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Professions)
            .ThenInclude(pp => pp.Profession)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? professions.FirstOrDefault(p => p.Id == id.Value)
            : professions.FirstOrDefault();

        var vm = new ProfessionDirectoryPageViewModel
        {
            Professions = professions,
            Players = players,
            SelectedProfession = selected
        };

        return View(vm);
    }

    [HttpGet("directory/classes")]
    public async Task<IActionResult> Classes(string? id, int? playerId)
    {
        var classes = Enum.GetValues<ClassType>();

        var players = await _db.PlayerProfiles
            .AsNoTracking()
            .Include(p => p.Professions)
            .ThenInclude(pp => pp.Profession)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var selectedClass = classes.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(id) && Enum.TryParse<ClassType>(id, true, out var parsedClass) && classes.Contains(parsedClass))
        {
            selectedClass = parsedClass;
        }

        var members = players.Where(p => p.ClassType == selectedClass).ToList();
        var selectedPlayer = playerId.HasValue
            ? members.FirstOrDefault(m => m.Id == playerId.Value)
            : members.FirstOrDefault();

        var stats = BuildPerformanceStats(selectedPlayer);

        var vm = new ClassDirectoryPageViewModel
        {
            Classes = classes,
            SelectedClass = selectedClass,
            MemberCounts = classes.ToDictionary(c => c, c => players.Count(p => p.ClassType == c)),
            Members = members,
            SelectedPlayer = selectedPlayer,
            SelectedPlayerHitCapPercent = stats.HitCapPercent,
            SelectedPlayerAverageDps = stats.AverageDps,
            SelectedPlayerCritChancePercent = stats.CritChancePercent,
            SelectedPlayerHastePercent = stats.HastePercent,
            SelectedPlayerPerformanceNote = stats.Note
        };

        return View(vm);
    }

    [HttpGet("directory/bosses")]
    public async Task<IActionResult> Bosses(int? id)
    {
        var bosses = await _db.BossGuides
            .AsNoTracking()
            .Include(b => b.RaidGuide)
            .OrderBy(b => b.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? bosses.FirstOrDefault(b => b.Id == id.Value)
            : bosses.FirstOrDefault();

        var vm = new BossDirectoryPageViewModel
        {
            Bosses = bosses,
            SelectedBoss = selected
        };

        return View(vm);
    }

    [HttpGet("directory/talents")]
    public async Task<IActionResult> Talents(int? id)
    {
        var talents = await _db.TalentBuilds
            .AsNoTracking()
            .Include(t => t.PlayerProfile)
            .OrderBy(t => t.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? talents.FirstOrDefault(t => t.Id == id.Value)
            : talents.FirstOrDefault();

        var vm = new TalentDirectoryPageViewModel
        {
            Talents = talents,
            SelectedTalent = selected
        };

        return View(vm);
    }

    [HttpGet("directory/player-professions")]
    public async Task<IActionResult> PlayerProfessions(int? playerId, int? professionId)
    {
        var links = await _db.PlayerProfessions
            .AsNoTracking()
            .Include(pp => pp.PlayerProfile)
            .Include(pp => pp.Profession)
            .OrderBy(pp => pp.PlayerProfileId)
            .ThenBy(pp => pp.ProfessionId)
            .ToListAsync();

        var selected = links.FirstOrDefault(pp =>
            (!playerId.HasValue || pp.PlayerProfileId == playerId.Value) &&
            (!professionId.HasValue || pp.ProfessionId == professionId.Value));

        var vm = new PlayerProfessionDirectoryPageViewModel
        {
            Links = links,
            SelectedLink = selected
        };

        return View(vm);
    }

    [HttpGet("directory/guilds")]
    public async Task<IActionResult> Guilds(int? id)
    {
        var guilds = await _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .OrderBy(g => g.Id)
            .ToListAsync();

        var selected = id.HasValue
            ? guilds.FirstOrDefault(g => g.Id == id.Value)
            : guilds.FirstOrDefault();

        var vm = new GuildDirectoryPageViewModel
        {
            Guilds = guilds,
            SelectedGuild = selected
        };

        return View(vm);
    }

    [HttpGet("directory/realms")]
    public async Task<IActionResult> Realms(string? id)
    {
        var guilds = await _db.Guilds
            .AsNoTracking()
            .Include(g => g.Members)
            .OrderBy(g => g.Realm)
            .ThenBy(g => g.Name)
            .ToListAsync();

        var realms = guilds
            .GroupBy(g => string.IsNullOrWhiteSpace(g.Realm) ? "Unknown" : g.Realm.Trim())
            .OrderBy(g => g.Key)
            .Select(group => new RealmSummary
            {
                Name = group.Key,
                GuildCount = group.Count(),
                MemberCount = group.Sum(g => g.Members.Count),
                OldestGuildCreatedAt = group.Min(g => g.CreatedAt),
                NewestGuildCreatedAt = group.Max(g => g.CreatedAt),
                Guilds = group.OrderBy(g => g.Name).ToList()
            })
            .ToList();

        var selected = !string.IsNullOrWhiteSpace(id)
            ? realms.FirstOrDefault(r => string.Equals(r.Name, id, StringComparison.OrdinalIgnoreCase))
            : realms.FirstOrDefault();

        var vm = new RealmDirectoryPageViewModel
        {
            Realms = realms,
            SelectedRealm = selected
        };

        return View(vm);
    }

    private static (double? HitCapPercent, int? AverageDps, double? CritChancePercent, double? HastePercent, string Note)
        BuildPerformanceStats(PlayerProfile? player)
    {
        if (player is null)
        {
            return (null, null, null, null, string.Empty);
        }

        var (baseHitCap, baseDps, baseCrit, baseHaste, classNote) = player.ClassType switch
        {
            ClassType.Warrior => (8.0, 5420, 34.7, 18.9, "Melee profile focused on stable uptime and armor penetration windows."),
            ClassType.Priest => (17.0, 4680, 29.4, 21.5, "Spell profile tuned for healer and utility consistency in raid rotations."),
            ClassType.Mage => (17.0, 6310, 41.8, 24.2, "Spell profile with strong crit scaling and cooldown burst windows."),
            ClassType.Rogue => (8.0, 6080, 39.2, 23.7, "Melee profile emphasizing poison uptime and cooldown chaining."),
            ClassType.Hunter => (8.0, 5875, 36.0, 20.9, "Ranged profile tuned for pet uptime and movement efficiency."),
            ClassType.Warlock => (17.0, 5960, 33.4, 18.6, "Caster profile tuned for DoT uptime and execute pressure."),
            ClassType.Paladin => (8.0, 5210, 31.1, 17.5, "Hybrid profile balancing utility cooldowns with steady throughput."),
            ClassType.DeathKnight => (8.0, 5750, 35.6, 19.8, "Melee profile focused on rune efficiency and disease maintenance."),
            ClassType.Shaman => (17.0, 5530, 32.9, 22.4, "Hybrid spell profile built around proc timing and totem utility."),
            ClassType.Druid => (17.0, 5470, 34.0, 20.6, "Hybrid profile balancing periodic effects with encounter utility."),
            _ => (8.0, 4500, 25.0, 15.0, "Baseline estimate for this class from mock data.")
        };

        var professionAverageSkill = player.Professions.Any()
            ? player.Professions.Average(pp => pp.SkillLevel)
            : 425.0;
        var professionDpsBonus = (int)Math.Round((professionAverageSkill - 400.0) * 3.25);

        var seed = Math.Abs(HashCode.Combine(player.Id, player.CharacterName, player.LastUpdatedAt.Day));
        var hitVariance = ((seed / 17) % 11 - 5) / 10.0;
        var critVariance = ((seed / 13) % 25 - 12) / 10.0;
        var hasteVariance = ((seed / 29) % 23 - 11) / 10.0;
        var dpsVariance = (seed % 801) - 400;

        var hitCap = Math.Round(Math.Clamp(baseHitCap + hitVariance, 5.0, 17.0), 1);
        var averageDps = Math.Max(2500, baseDps + professionDpsBonus + dpsVariance);
        var critChance = Math.Round(Math.Clamp(baseCrit + critVariance, 12.0, 55.0), 1);
        var haste = Math.Round(Math.Clamp(baseHaste + hasteVariance, 8.0, 35.0), 1);

        var note = $"{classNote} Profession avg skill {professionAverageSkill:0} contributes to this player's throughput profile.";

        return (hitCap, averageDps, critChance, haste, note);
    }
}
