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

        return player.ClassType switch
        {
            ClassType.Warrior => (8.0, 5420, 34.7, 18.9, "Melee hit cap reached; maintain armor penetration trinket uptime."),
            ClassType.Priest => (17.0, 4680, 29.4, 21.5, "Spell hit tuned for utility swaps; prioritize haste for smoother raid healing cycles."),
            ClassType.Mage => (17.0, 6310, 41.8, 24.2, "At spell hit cap with strong crit scaling; optimize cooldown alignment for burst windows."),
            _ => (8.0, 4500, 25.0, 15.0, "Baseline estimate for this class from mock data.")
        };
    }
}
