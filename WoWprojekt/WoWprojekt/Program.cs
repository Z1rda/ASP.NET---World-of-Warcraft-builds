using WoWprojekt.Models;
using WoWprojekt.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var blacksmithing = new Profession
{
    Id = 1,
    Name = "Blacksmithing",
    BenefitDescription = "Extra sockets for stronger tank gear."
};

var jewelcrafting = new Profession
{
    Id = 2,
    Name = "Jewelcrafting",
    BenefitDescription = "Powerful unique gems for stat optimization."
};

var tailoring = new Profession
{
    Id = 3,
    Name = "Tailoring",
    BenefitDescription = "Strong cloak enchants for casters."
};

var enchanting = new Profession
{
    Id = 4,
    Name = "Enchanting",
    BenefitDescription = "Ring enchants and flexible stat bonuses."
};

var alchemy = new Profession
{
    Id = 5,
    Name = "Alchemy",
    BenefitDescription = "Improved flask effects and flexible potion utility."
};

var engineering = new Profession
{
    Id = 6,
    Name = "Engineering",
    BenefitDescription = "Burst utility gadgets, glove enchants, and explosives."
};

var inscription = new Profession
{
    Id = 7,
    Name = "Inscription",
    BenefitDescription = "Strong shoulder inscriptions and utility glyph support."
};

var leatherworking = new Profession
{
    Id = 8,
    Name = "Leatherworking",
    BenefitDescription = "Powerful bracer enchants and armor craft options."
};

var mining = new Profession
{
    Id = 9,
    Name = "Mining",
    BenefitDescription = "Extra stamina bonus and dependable gathering support."
};

var herbalism = new Profession
{
    Id = 10,
    Name = "Herbalism",
    BenefitDescription = "Self-heal utility and consumable gathering synergy."
};

var skinning = new Profession
{
    Id = 11,
    Name = "Skinning",
    BenefitDescription = "Critical strike bonus and leather material support."
};

var tankPlayer = new PlayerProfile
{
    Id = 1,
    CharacterName = "ArthasShield",
    Level = 80,
    ClassType = ClassType.Warrior,
    LastUpdatedAt = DateTime.UtcNow,
    TalentBuilds = new List<TalentBuild>
    {
        new() { Id = 1, BuildName = "Protection Main Tank", TalentCode = "15/5/51", Description = "High mitigation for progression bosses.", PlayerProfileId = 1 },
        new() { Id = 2, BuildName = "Protection Utility", TalentCode = "18/3/50", Description = "More raid utility and interrupt control.", PlayerProfileId = 1 }
    },
    Professions = new List<PlayerProfession>
    {
        new() { PlayerProfileId = 1, ProfessionId = 1, Profession = blacksmithing, SkillLevel = 450 },
        new() { PlayerProfileId = 1, ProfessionId = 2, Profession = jewelcrafting, SkillLevel = 444 }
    }
};

var healerPlayer = new PlayerProfile
{
    Id = 2,
    CharacterName = "Lightweaver",
    Level = 80,
    ClassType = ClassType.Priest,
    LastUpdatedAt = DateTime.UtcNow,
    TalentBuilds = new List<TalentBuild>
    {
        new() { Id = 3, BuildName = "Holy Raid Healing", TalentCode = "14/57/0", Description = "Strong group healing and cooldown support.", PlayerProfileId = 2 },
        new() { Id = 4, BuildName = "Discipline Mitigation", TalentCode = "57/14/0", Description = "Absorb-focused setup for predictable damage.", PlayerProfileId = 2 }
    },
    Professions = new List<PlayerProfession>
    {
        new() { PlayerProfileId = 2, ProfessionId = 3, Profession = tailoring, SkillLevel = 447 },
        new() { PlayerProfileId = 2, ProfessionId = 4, Profession = enchanting, SkillLevel = 439 }
    }
};

var damagePlayer = new PlayerProfile
{
    Id = 3,
    CharacterName = "Frostburst",
    Level = 80,
    ClassType = ClassType.Mage,
    LastUpdatedAt = DateTime.UtcNow,
    TalentBuilds = new List<TalentBuild>
    {
        new() { Id = 5, BuildName = "Fire Single Target", TalentCode = "18/53/0", Description = "Best sustained damage on long encounters.", PlayerProfileId = 3 },
        new() { Id = 6, BuildName = "Arcane Burst", TalentCode = "57/3/11", Description = "High burst windows and mana management.", PlayerProfileId = 3 }
    },
    Professions = new List<PlayerProfession>
    {
        new() { PlayerProfileId = 3, ProfessionId = 3, Profession = tailoring, SkillLevel = 450 },
        new() { PlayerProfileId = 3, ProfessionId = 4, Profession = enchanting, SkillLevel = 446 }
    }
};

var allProfessionsForGeneration = new List<Profession>
{
    blacksmithing,
    jewelcrafting,
    tailoring,
    enchanting,
    alchemy,
    engineering,
    inscription,
    leatherworking,
    mining,
    herbalism,
    skinning
};

//generiranje svih mogucih parova profesija, svaki igrac ima 2 profesije
var professionPairs = new List<(Profession First, Profession Second)>();
for (var i = 0; i < allProfessionsForGeneration.Count; i++)
{
    for (var j = i + 1; j < allProfessionsForGeneration.Count; j++)
    {
        professionPairs.Add((allProfessionsForGeneration[i], allProfessionsForGeneration[j]));
    }
}

var extraPlayers = new List<PlayerProfile>();
var nextPlayerId = 4;
var nextTalentId = 7;
var pairIndex = 0;

var classRosterNames = new Dictionary<ClassType, string[]>
{
    [ClassType.DeathKnight] = new[] { "Gravewarden", "Runebreaker", "DuskHarbinger", "FrostMourner", "SoulCarver" },
    [ClassType.Druid] = new[] { "Moonbark", "Wildmantle", "Rootseer", "Starclaw", "Oakwhisper" },
    [ClassType.Hunter] = new[] { "RimeTracker", "IronQuiver", "Wolffletcher", "Farseeker", "Ashstalker" },
    [ClassType.Mage] = new[] { "EmberSavant", "Spellfrost", "Runeweft", "Arcflare", "Glimmerhex" },
    [ClassType.Paladin] = new[] { "Lightwarden", "AureusHammer", "DawnVow", "SilverAegis", "Sunmarshal" },
    [ClassType.Priest] = new[] { "Veilkeeper", "Luminae", "MindCantor", "Faithmender", "StarConfessor" },
    [ClassType.Rogue] = new[] { "Nightshiv", "Shadecoil", "RavenEdge", "SilentViper", "Duskcut" },
    [ClassType.Shaman] = new[] { "Stormtotem", "Farsea", "Earthcaller", "SkyRitual", "Thunderbind" },
    [ClassType.Warlock] = new[] { "Felscribe", "DreadWarden", "Hexbinder", "SoulAsh", "VoidCant" },
    [ClassType.Warrior] = new[] { "Ironthane", "Skullbreaker", "Shieldmaul", "Stonebanner", "Warbrand" }
};

foreach (var classType in Enum.GetValues<ClassType>())
{
    var names = classRosterNames[classType];
    for (var variant = 0; variant < names.Length; variant++)
    {
        var assignedProfessions = professionPairs[pairIndex++];
        var playerId = nextPlayerId++;
        var rank = variant + 1;
        var firstProfessionSkill = 405 + ((playerId * 11 + assignedProfessions.First.Id * 7) % 46);
        var secondProfessionSkill = 405 + ((playerId * 13 + assignedProfessions.Second.Id * 5) % 46);

        var generatedPlayer = new PlayerProfile
        {
            Id = playerId,
            CharacterName = names[variant],
            Level = 80,
            ClassType = classType,
            LastUpdatedAt = DateTime.UtcNow.AddDays(-rank),
            TalentBuilds = new List<TalentBuild>
            {
                new()
                {
                    Id = nextTalentId++,
                    BuildName = $"{classType} Raid Focus {rank}",
                    TalentCode = $"{12 + rank}/{13 + ((int)classType % 8)}/{46 - rank}",
                    Description = $"Primary raid build for {classType} with emphasis on throughput and encounter consistency.",
                    PlayerProfileId = playerId
                },
                new()
                {
                    Id = nextTalentId++,
                    BuildName = $"{classType} Utility Focus {rank}",
                    TalentCode = $"{10 + rank}/{18 + ((int)classType % 7)}/{42 - rank}",
                    Description = $"Utility and control variant for {classType} used in movement-heavy encounters.",
                    PlayerProfileId = playerId
                }
            },
            Professions = new List<PlayerProfession>
            {
                new()
                {
                    PlayerProfileId = playerId,
                    ProfessionId = assignedProfessions.First.Id,
                    Profession = assignedProfessions.First,
                    SkillLevel = firstProfessionSkill
                },
                new()
                {
                    PlayerProfileId = playerId,
                    ProfessionId = assignedProfessions.Second.Id,
                    Profession = assignedProfessions.Second,
                    SkillLevel = secondProfessionSkill
                }
            }
        };

        extraPlayers.Add(generatedPlayer);
    }
}

var players = new List<PlayerProfile> { tankPlayer, healerPlayer, damagePlayer };
players.AddRange(extraPlayers);

var guilds = new List<Guild>
{
    new()
    {
        Id = 1,
        Name = "Northern Vigil",
        Realm = "Icecrown",
        CreatedAt = DateTime.UtcNow,
        Members = players.ToList()
    }
};

foreach (var player in players)
{
    player.GuildId = guilds[0].Id;
    player.Guild = guilds[0];
}

var raidGuides = new List<RaidGuide>
{
    new()
    {
        Id = 1,
        RaidName = "Icecrown Citadel",
        PreparationNotes = "Bring frost resistance options and assign Defile positions.",
        UpdatedAt = DateTime.UtcNow,
        Bosses = new List<BossGuide>
        {
            new()
            {
                Id = 1,
                BossName = "Lord Marrowgar",
                DifficultyRating = 6,
                RaidGuideId = 1,
                Tactics = "Tank: swap on Bone Slice and keep boss centered. Healer: stabilize raid during Bone Storm pulses. Damage Dealer: spread for Coldflame lanes and burst Bone Spikes immediately."
            },
            new()
            {
                Id = 2,
                BossName = "Lady Deathwhisper",
                DifficultyRating = 7,
                RaidGuideId = 1,
                Tactics = "Tank: control adds by side and swap to boss in phase two. Healer: watch tank spikes from empowered adds. Damage Dealer: prioritize caster adds and interrupt dangerous volleys."
            },
            new()
            {
                Id = 3,
                BossName = "Gunship Battle",
                DifficultyRating = 4,
                RaidGuideId = 1,
                Tactics = "Tank: secure enemy commander and boarding packs. Healer: keep board team stable during rocket exchanges. Damage Dealer: execute fast jumps and clear enemy deck quickly."
            },
            new()
            {
                Id = 4,
                BossName = "Deathbringer Saurfang",
                DifficultyRating = 7,
                RaidGuideId = 1,
                Tactics = "Tank: taunt swap at Rune of Blood. Healer: prepare for Mark of the Fallen Champion pressure. Damage Dealer: slow and kill Blood Beasts before they reach players."
            },
            new()
            {
                Id = 5,
                BossName = "Festergut",
                DifficultyRating = 7,
                RaidGuideId = 1,
                Tactics = "Tank: rotate cooldowns for high inhale stacks. Healer: ramp raid throughput for Pungent Blight. Damage Dealer: maintain uptime while respecting spore positioning."
            },
            new()
            {
                Id = 6,
                BossName = "Rotface",
                DifficultyRating = 7,
                RaidGuideId = 1,
                Tactics = "Tank: kite large ooze cleanly around the room edge. Healer: cover infection targets while moving. Damage Dealer: avoid ooze explosions and keep boss pressure consistent."
            },
            new()
            {
                Id = 7,
                BossName = "Professor Putricide",
                DifficultyRating = 8,
                RaidGuideId = 1,
                Tactics = "Tank: swap at high debuff stacks and manage abomination support. Healer: spot heal volatile experiment targets quickly. Damage Dealer: switch fast to slimes and maintain phase transitions cleanly."
            },
            new()
            {
                Id = 8,
                BossName = "Blood Prince Council",
                DifficultyRating = 8,
                RaidGuideId = 1,
                Tactics = "Tank: split princes correctly and control kinetic bombs. Healer: monitor ranged spread damage. Damage Dealer: target empowered prince and avoid shadow prisons."
            },
            new()
            {
                Id = 9,
                BossName = "Blood-Queen Lana'thel",
                DifficultyRating = 8,
                RaidGuideId = 1,
                Tactics = "Tank: hold position for clean air phases. Healer: coordinate cooldowns for raid-wide pulses. Damage Dealer: execute bite order and spread for Pact targets."
            },
            new()
            {
                Id = 10,
                BossName = "Valithria Dreamwalker",
                DifficultyRating = 6,
                RaidGuideId = 1,
                Tactics = "Tank: control add waves and suppress dangerous casters. Healer: portal rotation to maximize Dreamwalker healing. Damage Dealer: prioritize blazing skeletons and suppressors."
            },
            new()
            {
                Id = 11,
                BossName = "Sindragosa",
                DifficultyRating = 9,
                RaidGuideId = 1,
                Tactics = "Tank: rotate cooldowns for Frost Breath and debuff stacks. Healer: manage line-of-sight ice tomb moments. Damage Dealer: stop casts on Mystic Buffet reset timings."
            },
            new()
            {
                Id = 12,
                BossName = "The Lich King",
                DifficultyRating = 10,
                RaidGuideId = 1,
                Tactics = "Tank: swap on Soul Reaper and control Raging Spirits. Healer: pre-shield before Infest and stabilize during transition damage. Damage Dealer: switch to Val'kyr instantly and spread for Defile."
            }
        }
    },
    new()
    {
        Id = 2,
        RaidName = "Trial of the Grand Crusader",
        PreparationNotes = "Prepare cooldown rotations for multi-phase gauntlet and assign strict focus targets for each encounter.",
        UpdatedAt = DateTime.UtcNow,
        Bosses = new List<BossGuide>
        {
            new()
            {
                Id = 13,
                BossName = "Northrend Beasts",
                DifficultyRating = 8,
                RaidGuideId = 2,
                Tactics = "Tank: swap cleanly on Gormok and control worms with clear positioning. Healer: prepare for puncture stacks and raid spikes from toxins. Damage Dealer: prioritize snobolds and execute mobile uptime during Icehowl charges."
            },
            new()
            {
                Id = 14,
                BossName = "Lord Jaraxxus",
                DifficultyRating = 8,
                RaidGuideId = 2,
                Tactics = "Tank: keep Jaraxxus stable and position infernals away from raid. Healer: aggressively heal Incinerate Flesh targets. Damage Dealer: instantly switch to portals and volcano spawns, then return to boss."
            },
            new()
            {
                Id = 15,
                BossName = "Faction Champions",
                DifficultyRating = 8,
                RaidGuideId = 2,
                Tactics = "Tank: peel melee pressure and disrupt enemy burst windows. Healer: survive focused crowd control and rotate personal defensives. Damage Dealer: follow coordinated kill target calls with heavy interruption pressure."
            },
            new()
            {
                Id = 16,
                BossName = "Twin Val'kyr",
                DifficultyRating = 9,
                RaidGuideId = 2,
                Tactics = "Tank: keep twins controlled for predictable movement lanes. Healer: handle spike damage during vortexes and empowered surges. Damage Dealer: match light/dark essence correctly and swap for shield break timings."
            },
            new()
            {
                Id = 21,
                BossName = "Anub'arak",
                DifficultyRating = 9,
                RaidGuideId = 2,
                Tactics = "Tank: position boss away from raid and pick up burrow adds quickly. Healer: stabilize raid during Leeching Swarm with controlled throughput. Damage Dealer: burn adds fast and manage phase pushes with strict target discipline."
            }
        }
    },
    new()
    {
        Id = 3,
        RaidName = "Ruby Sanctum",
        PreparationNotes = "Prepare movement assignments for cutter beams and twilight realm transitions.",
        UpdatedAt = DateTime.UtcNow,
        Bosses = new List<BossGuide>
        {
            new()
            {
                Id = 17,
                BossName = "Baltharus the Warborn",
                DifficultyRating = 7,
                RaidGuideId = 3,
                Tactics = "Tank: taunt clone quickly and separate targets. Healer: split attention between clone and main tank groups. Damage Dealer: focus main target and cleave clone when safe."
            },
            new()
            {
                Id = 18,
                BossName = "Saviana Ragefire",
                DifficultyRating = 7,
                RaidGuideId = 3,
                Tactics = "Tank: keep boss stable for predictable flame breath direction. Healer: cover flame beacon targets immediately. Damage Dealer: spread early and collapse only when safe."
            },
            new()
            {
                Id = 19,
                BossName = "General Zarithrian",
                DifficultyRating = 8,
                RaidGuideId = 3,
                Tactics = "Tank: split adds and boss with clear taunt communication. Healer: prioritize tanks during add spikes. Damage Dealer: burn adds on spawn before returning to boss."
            },
            new()
            {
                Id = 20,
                BossName = "Halion",
                DifficultyRating = 9,
                RaidGuideId = 3,
                Tactics = "Tank: maintain split-realm threat and rotate cooldowns for breath combos. Healer: coordinate healing between physical and twilight realms. Damage Dealer: avoid cutter beams and balance damage across both realms."
            }
        }
    }
};

foreach (var raid in raidGuides)
{
    foreach (var boss in raid.Bosses)
    {
        boss.RaidGuide = raid;
        boss.BossImageUrl = BuildBossImageUrl(boss.BossName);
    }
}

foreach (var player in players)
{
    foreach (var build in player.TalentBuilds)
    {
        build.PlayerProfile = player;
    }

    foreach (var playerProfession in player.Professions)
    {
        playerProfession.PlayerProfile = player;
    }
}

var professions = new List<Profession>
{
    blacksmithing,
    jewelcrafting,
    tailoring,
    enchanting,
    alchemy,
    engineering,
    inscription,
    leatherworking,
    mining,
    herbalism,
    skinning
};

foreach (var profession in professions)
{
    profession.Players = players
        .SelectMany(p => p.Professions)
        .Where(pp => pp.ProfessionId == profession.Id)
        .ToList();
}

builder.Services.AddSingleton<IMockRepository>(
    new MockRepository(players, raidGuides, professions, guilds));

Console.WriteLine($"Loaded {players.Count} player characters, {players.Sum(p => p.TalentBuilds.Count)} talent builds, {players.Sum(p => p.Professions.Count)} profession links.");
Console.WriteLine($"Loaded {raidGuides.Count} raid guides with bosses: {string.Join(", ", raidGuides.SelectMany(r => r.Bosses).Select(b => b.BossName))}.");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

static string BuildBossImageUrl(string bossName)
{
    var bossImageMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["lordmarrowgar"] = "lord-marrowgar.jpg",
        ["ladydeathwhisper"] = "lady-deathwhisper.jpg",
        ["gunshipbattle"] = "gunship.jpg",
        ["deathbringersaurfang"] = "deathbringer-saurfang.jpg",
        ["festergut"] = "festergut.jpg",
        ["rotface"] = "rotface.jpg",
        ["professorputricide"] = "professor-putricide.jpg",
        ["bloodprincecouncil"] = "blood-prince-council.jpg",
        ["bloodqueenlanathel"] = "blood-queen-lana'thel.jpg",
        ["valithriadreamwalker"] = "valithria-dreamwalker.jpg",
        ["sindragosa"] = "sindragosa.jpg",
        ["thelichking"] = "the-lich-king.jpg",
        ["northrendbeasts"] = "beasts-of-northrend.jpg",
        ["lordjaraxxus"] = "lord-jaraxxus.jpg",
        ["factionchampions"] = "Faction_Champions.jpg",
        ["twinvalkyr"] = "twin-val'kyr.jpg",
        ["anubarak"] = "anub'arak.jpg",
        ["halion"] = "halion.jpg"
    };

    var normalizedBossName = string.Concat(bossName.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    if (bossImageMap.TryGetValue(normalizedBossName, out var fileName))
    {
        return $"/images/{Uri.EscapeDataString(fileName)}";
    }

    var encodedName = Uri.EscapeDataString(bossName);
    return $"https://placehold.co/96x96/1c1f24/e6dec6?text={encodedName}";
}


app.Run();
