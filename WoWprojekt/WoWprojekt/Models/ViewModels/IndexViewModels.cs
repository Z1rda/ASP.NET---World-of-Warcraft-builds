using WoWprojekt.Models;

namespace WoWprojekt.Models.ViewModels;

public class PlayerProfileIndexViewModel
{
    public IReadOnlyList<PlayerProfile> Players { get; set; } = Array.Empty<PlayerProfile>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class BossGuideIndexViewModel
{
    public IReadOnlyList<BossGuide> Bosses { get; set; } = Array.Empty<BossGuide>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class RaidGuideIndexViewModel
{
    public IReadOnlyList<RaidGuide> Raids { get; set; } = Array.Empty<RaidGuide>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class GuildIndexViewModel
{
    public IReadOnlyList<Guild> Guilds { get; set; } = Array.Empty<Guild>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class ProfessionIndexViewModel
{
    public IReadOnlyList<Profession> Professions { get; set; } = Array.Empty<Profession>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class TalentBuildIndexViewModel
{
    public IReadOnlyList<TalentBuild> TalentBuilds { get; set; } = Array.Empty<TalentBuild>();
    public string SearchQuery { get; set; } = string.Empty;
}

public class PlayerProfessionIndexViewModel
{
    public IReadOnlyList<PlayerProfession> PlayerProfessions { get; set; } = Array.Empty<PlayerProfession>();
    public string SearchQuery { get; set; } = string.Empty;
}
