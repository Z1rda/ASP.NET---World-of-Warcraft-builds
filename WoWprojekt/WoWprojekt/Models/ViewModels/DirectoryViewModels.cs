namespace WoWprojekt.Models.ViewModels;

public class PlayerDirectoryPageViewModel
{
    public IReadOnlyList<PlayerProfile> Players { get; set; } = Array.Empty<PlayerProfile>();
    public PlayerProfile? SelectedPlayer { get; set; }
}

public class RaidDirectoryPageViewModel
{
    public IReadOnlyList<RaidGuide> Raids { get; set; } = Array.Empty<RaidGuide>();
    public RaidGuide? SelectedRaid { get; set; }
    public IReadOnlyList<BossGuide> VisibleBosses { get; set; } = Array.Empty<BossGuide>();
    public IReadOnlySet<int> SelectedBossIds { get; set; } = new HashSet<int>();
    public bool FilterApplied { get; set; }
}

public class ProfessionDirectoryPageViewModel
{
    public IReadOnlyList<Profession> Professions { get; set; } = Array.Empty<Profession>();
    public IReadOnlyList<PlayerProfile> Players { get; set; } = Array.Empty<PlayerProfile>();
    public Profession? SelectedProfession { get; set; }
}

public class ClassDirectoryPageViewModel
{
    public IReadOnlyList<ClassType> Classes { get; set; } = Array.Empty<ClassType>();
    public ClassType SelectedClass { get; set; }
    public IReadOnlyDictionary<ClassType, int> MemberCounts { get; set; } = new Dictionary<ClassType, int>();
    public IReadOnlyList<PlayerProfile> Members { get; set; } = Array.Empty<PlayerProfile>();
    public PlayerProfile? SelectedPlayer { get; set; }
    public double? SelectedPlayerHitCapPercent { get; set; }
    public int? SelectedPlayerAverageDps { get; set; }
    public double? SelectedPlayerCritChancePercent { get; set; }
    public double? SelectedPlayerHastePercent { get; set; }
    public string SelectedPlayerPerformanceNote { get; set; } = string.Empty;
}

public class BossDirectoryPageViewModel
{
    public IReadOnlyList<BossGuide> Bosses { get; set; } = Array.Empty<BossGuide>();
    public BossGuide? SelectedBoss { get; set; }
}

public class TalentDirectoryPageViewModel
{
    public IReadOnlyList<TalentBuild> Talents { get; set; } = Array.Empty<TalentBuild>();
    public TalentBuild? SelectedTalent { get; set; }
}

public class PlayerProfessionDirectoryPageViewModel
{
    public IReadOnlyList<PlayerProfession> Links { get; set; } = Array.Empty<PlayerProfession>();
    public PlayerProfession? SelectedLink { get; set; }
}

public class GuildDirectoryPageViewModel
{
    public IReadOnlyList<Guild> Guilds { get; set; } = Array.Empty<Guild>();
    public Guild? SelectedGuild { get; set; }
}
