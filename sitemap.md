# Sitemap

## Home
- / (default route) -> HomeController.Index -> [WoWprojekt/Views/Home/Index.cshtml](WoWprojekt/Views/Home/Index.cshtml)
- /Home/Index (default route) -> HomeController.Index -> [WoWprojekt/Views/Home/Index.cshtml](WoWprojekt/Views/Home/Index.cshtml)
- /Home/Privacy (default route) -> HomeController.Privacy -> [WoWprojekt/Views/Home/Privacy.cshtml](WoWprojekt/Views/Home/Privacy.cshtml)
- /Home/Error (default route) -> HomeController.Error -> [WoWprojekt/Views/Shared/Error.cshtml](WoWprojekt/Views/Shared/Error.cshtml)

## Encyclopedia
- /directory/players (custom route) -> EncyclopediaController.Players -> [WoWprojekt/Views/Encyclopedia/Players.cshtml](WoWprojekt/Views/Encyclopedia/Players.cshtml)
- /directory/raids (custom route) -> EncyclopediaController.Raids -> [WoWprojekt/Views/Encyclopedia/Raids.cshtml](WoWprojekt/Views/Encyclopedia/Raids.cshtml)
- /directory/professions (custom route) -> EncyclopediaController.Professions -> [WoWprojekt/Views/Encyclopedia/Professions.cshtml](WoWprojekt/Views/Encyclopedia/Professions.cshtml)
- /directory/classes (custom route) -> EncyclopediaController.Classes -> [WoWprojekt/Views/Encyclopedia/Classes.cshtml](WoWprojekt/Views/Encyclopedia/Classes.cshtml)
- /directory/bosses (custom route) -> EncyclopediaController.Bosses -> [WoWprojekt/Views/Encyclopedia/Bosses.cshtml](WoWprojekt/Views/Encyclopedia/Bosses.cshtml)
- /directory/talents (custom route) -> EncyclopediaController.Talents -> [WoWprojekt/Views/Encyclopedia/Talents.cshtml](WoWprojekt/Views/Encyclopedia/Talents.cshtml)
- /directory/player-professions (custom route) -> EncyclopediaController.PlayerProfessions -> [WoWprojekt/Views/Encyclopedia/PlayerProfessions.cshtml](WoWprojekt/Views/Encyclopedia/PlayerProfessions.cshtml)
- /directory/guilds (custom route) -> EncyclopediaController.Guilds -> [WoWprojekt/Views/Encyclopedia/Guilds.cshtml](WoWprojekt/Views/Encyclopedia/Guilds.cshtml)

## Query parameters
- Players: ?id={playerId}
- Raids: ?id={raidId}&selectedBossIds={bossId}&filterApplied={true|false}
- Professions: ?id={professionId}
- Classes: ?id={className}&playerId={playerId}
- Bosses: ?id={bossId}
- Talents: ?id={talentId}
- PlayerProfessions: ?playerId={playerId}&professionId={professionId}
- Guilds: ?id={guildId}
- Realms: ?id={realmName}
