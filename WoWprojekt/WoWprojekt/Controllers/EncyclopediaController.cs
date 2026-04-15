using Microsoft.AspNetCore.Mvc;
using WoWprojekt.Models;
using WoWprojekt.Models.ViewModels;
using WoWprojekt.Services;

namespace WoWprojekt.Controllers;

public class EncyclopediaController : Controller
{
    private readonly IMockRepository _repository;

    public EncyclopediaController(IMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Players(int? id)
    {
        var selected = id.HasValue
            ? _repository.Players.FirstOrDefault(p => p.Id == id.Value)
            : _repository.Players.FirstOrDefault();

        var vm = new PlayerDirectoryPageViewModel
        {
            Players = _repository.Players,
            SelectedPlayer = selected
        };

        return View(vm);
    }

    public IActionResult Raids(int? id, List<int>? selectedBossIds, bool filterApplied = false)
    {
        var selected = id.HasValue
            ? _repository.Raids.FirstOrDefault(r => r.Id == id.Value)
            : _repository.Raids.FirstOrDefault();

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
            Raids = _repository.Raids,
            SelectedRaid = selected,
            VisibleBosses = visibleBosses,
            SelectedBossIds = sanitizedSelectedIds,
            FilterApplied = filterApplied
        };

        return View(vm);
    }

    public IActionResult Professions(int? id)
    {
        var selected = id.HasValue
            ? _repository.Professions.FirstOrDefault(p => p.Id == id.Value)
            : _repository.Professions.FirstOrDefault();

        var vm = new ProfessionDirectoryPageViewModel
        {
            Professions = _repository.Professions,
            Players = _repository.Players,
            SelectedProfession = selected
        };

        return View(vm);
    }

    public IActionResult Classes(string? id, int? playerId)
    {
        var classes = _repository.Classes;

        var selectedClass = classes.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(id) && Enum.TryParse<ClassType>(id, true, out var parsedClass) && classes.Contains(parsedClass))
        {
            selectedClass = parsedClass;
        }

        var members = _repository.Players.Where(p => p.ClassType == selectedClass).ToList();
        var selectedPlayer = playerId.HasValue
            ? members.FirstOrDefault(m => m.Id == playerId.Value)
            : members.FirstOrDefault();

        var stats = BuildPerformanceStats(selectedPlayer);

        var vm = new ClassDirectoryPageViewModel
        {
            Classes = classes,
            SelectedClass = selectedClass,
            MemberCounts = classes.ToDictionary(c => c, c => _repository.Players.Count(p => p.ClassType == c)),
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

    public IActionResult Bosses(int? id)
    {
        var selected = id.HasValue
            ? _repository.Bosses.FirstOrDefault(b => b.Id == id.Value)
            : _repository.Bosses.FirstOrDefault();

        var vm = new BossDirectoryPageViewModel
        {
            Bosses = _repository.Bosses,
            SelectedBoss = selected
        };

        return View(vm);
    }

    public IActionResult Talents(int? id)
    {
        var selected = id.HasValue
            ? _repository.TalentBuilds.FirstOrDefault(t => t.Id == id.Value)
            : _repository.TalentBuilds.FirstOrDefault();

        var vm = new TalentDirectoryPageViewModel
        {
            Talents = _repository.TalentBuilds,
            SelectedTalent = selected
        };

        return View(vm);
    }

    public IActionResult PlayerProfessions(int? playerId, int? professionId)
    {
        var selected = _repository.PlayerProfessions.FirstOrDefault(pp =>
            (!playerId.HasValue || pp.PlayerProfileId == playerId.Value) &&
            (!professionId.HasValue || pp.ProfessionId == professionId.Value));

        var vm = new PlayerProfessionDirectoryPageViewModel
        {
            Links = _repository.PlayerProfessions,
            SelectedLink = selected
        };

        return View(vm);
    }

    public IActionResult Guilds(int? id)
    {
        var selected = id.HasValue
            ? _repository.Guilds.FirstOrDefault(g => g.Id == id.Value)
            : _repository.Guilds.FirstOrDefault();

        var vm = new GuildDirectoryPageViewModel
        {
            Guilds = _repository.Guilds,
            SelectedGuild = selected
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
