using WoWprojekt.Models;

namespace WoWprojekt.Services;

public class MockRepository : IMockRepository
{
    public MockRepository(
        IReadOnlyList<PlayerProfile> players,
        IReadOnlyList<RaidGuide> raids,
        IReadOnlyList<Profession> professions,
        IReadOnlyList<Guild> guilds)
    {
        Players = players;
        Raids = raids;
        Professions = professions;
        Guilds = guilds;

        Bosses = raids.SelectMany(r => r.Bosses).ToList();
        TalentBuilds = players.SelectMany(p => p.TalentBuilds).ToList();
        PlayerProfessions = players.SelectMany(p => p.Professions).ToList();
        Classes = Enum.GetValues<ClassType>();
    }

    public IReadOnlyList<PlayerProfile> Players { get; }
    public IReadOnlyList<RaidGuide> Raids { get; }
    public IReadOnlyList<Profession> Professions { get; }
    public IReadOnlyList<Guild> Guilds { get; }
    public IReadOnlyList<BossGuide> Bosses { get; }
    public IReadOnlyList<TalentBuild> TalentBuilds { get; }
    public IReadOnlyList<PlayerProfession> PlayerProfessions { get; }
    public IReadOnlyList<ClassType> Classes { get; }
}
