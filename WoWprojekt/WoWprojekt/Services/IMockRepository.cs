using WoWprojekt.Models;

namespace WoWprojekt.Services;

public interface IMockRepository
{
    IReadOnlyList<PlayerProfile> Players { get; }
    IReadOnlyList<RaidGuide> Raids { get; }
    IReadOnlyList<Profession> Professions { get; }
    IReadOnlyList<Guild> Guilds { get; }
    IReadOnlyList<BossGuide> Bosses { get; }
    IReadOnlyList<TalentBuild> TalentBuilds { get; }
    IReadOnlyList<PlayerProfession> PlayerProfessions { get; }
    IReadOnlyList<ClassType> Classes { get; }
}
