using WoWprojekt.Models;

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
        new() { PlayerProfileId = 1, ProfessionId = 2, Profession = jewelcrafting, SkillLevel = 450 }
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
        new() { PlayerProfileId = 2, ProfessionId = 3, Profession = tailoring, SkillLevel = 450 },
        new() { PlayerProfileId = 2, ProfessionId = 4, Profession = enchanting, SkillLevel = 450 }
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
        new() { PlayerProfileId = 3, ProfessionId = 4, Profession = enchanting, SkillLevel = 450 }
    }
};

var players = new List<PlayerProfile> { tankPlayer, healerPlayer, damagePlayer };

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
        RaidName = "Trial of the Champion",
        PreparationNotes = "Use controlled interrupts and clear add priority before boss focus.",
        UpdatedAt = DateTime.UtcNow,
        Bosses = new List<BossGuide>
        {
            new()
            {
                Id = 2,
                BossName = "Anub'arak",
                DifficultyRating = 8,
                RaidGuideId = 2,
                Tactics = "Tank: position boss away from raid and pick up burrow adds quickly. Healer: keep raid stable during Penetrating Cold. Damage Dealer: burn adds fast and push phases cleanly."
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
                Id = 3,
                BossName = "Halion",
                DifficultyRating = 9,
                RaidGuideId = 3,
                Tactics = "Tank: maintain split-realm threat and rotate cooldowns for breath combos. Healer: coordinate healing between physical and twilight realms. Damage Dealer: avoid cutter beams and balance damage across both realms."
            }
        }
    }
};

Console.WriteLine($"Loaded {players.Count} player characters, {players.Sum(p => p.TalentBuilds.Count)} talent builds, {players.Sum(p => p.Professions.Count)} profession links.");
Console.WriteLine($"Loaded {raidGuides.Count} raid guides with bosses: {string.Join(", ", raidGuides.SelectMany(r => r.Bosses).Select(b => b.BossName))}.");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
