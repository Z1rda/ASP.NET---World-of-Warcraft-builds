using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;
using Xunit;

namespace WoWprojekt.Tests;

public class PlayerProfessionsApiControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static WebApplicationFactory<Program> CreateFactory(string dbName)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Google:ClientId"] = "test",
                        ["Authentication:Google:ClientSecret"] = "test"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    services.AddAuthentication(opts =>
                    {
                        opts.DefaultAuthenticateScheme = "Test";
                        opts.DefaultChallengeScheme = "Test";
                        opts.DefaultForbidScheme = "Test";
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                });
            });
    }

    private static (PlayerProfile player, Profession profession) SeedPlayerAndProfession(ApplicationDbContext db)
    {
        var player = new PlayerProfile
        {
            CharacterName = "Arthas",
            Level = 80,
            ClassType = ClassType.DeathKnight,
            LastUpdatedAt = DateTime.UtcNow
        };
        var profession = new Profession { Name = "Alchemy", BenefitDescription = "Craft potions" };
        db.PlayerProfiles.Add(player);
        db.Professions.Add(profession);
        db.SaveChanges();
        return (player, profession);
    }

    private static PlayerProfession SeedLink(ApplicationDbContext db, int playerProfileId, int professionId)
    {
        var link = new PlayerProfession
        {
            PlayerProfileId = playerProfileId,
            ProfessionId = professionId,
            SkillLevel = 300
        };
        db.PlayerProfessions.Add(link);
        db.SaveChanges();
        return link;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            SeedLink(db, player.Id, profession.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfessionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            seeded = SeedLink(db, player.Id, profession.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/playerprofessions/{seeded.PlayerProfileId}/{seeded.ProfessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PlayerProfessionDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seeded.PlayerProfileId, dto.PlayerProfileId);
        Assert.Equal(seeded.ProfessionId, dto.ProfessionId);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofessions/99999/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId, professionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            playerId = player.Id;
            professionId = profession.Id;
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = playerId,
            ProfessionId = professionId,
            SkillLevel = 200
        };

        var response = await client.PostAsJsonAsync("/api/playerprofessions", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlayerProfessionDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(playerId, created.PlayerProfileId);
        Assert.Equal(professionId, created.ProfessionId);
    }

    [Fact]
    public async Task Post_Returns400_WhenPlayerDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int professionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profession = new Profession { Name = "Alchemy", BenefitDescription = "Craft potions" };
            db.Professions.Add(profession);
            db.SaveChanges();
            professionId = profession.Id;
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = 99999,
            ProfessionId = professionId,
            SkillLevel = 100
        };

        var response = await client.PostAsJsonAsync("/api/playerprofessions", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            seeded = SeedLink(db, player.Id, profession.Id);
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = seeded.PlayerProfileId,
            ProfessionId = seeded.ProfessionId,
            SkillLevel = 450
        };

        var response = await client.PutAsJsonAsync(
            $"/api/playerprofessions/{seeded.PlayerProfileId}/{seeded.ProfessionId}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentLink()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = 99999,
            ProfessionId = 99999,
            SkillLevel = 100
        };

        var response = await client.PutAsJsonAsync("/api/playerprofessions/99999/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            seeded = SeedLink(db, player.Id, profession.Id);
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync(
            $"/api/playerprofessions/{seeded.PlayerProfileId}/{seeded.ProfessionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentLink()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/playerprofessions/99999/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FilterByPlayerProfileId_ReturnsOnlyThatPlayersLinks()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int player2Id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player1, profession) = SeedPlayerAndProfession(db);
            SeedLink(db, player1.Id, profession.Id);
            var player2 = new PlayerProfile
            {
                CharacterName = "Thrall",
                Level = 80,
                ClassType = ClassType.Shaman,
                LastUpdatedAt = DateTime.UtcNow
            };
            db.PlayerProfiles.Add(player2);
            db.SaveChanges();
            player2Id = player2.Id;
            db.PlayerProfessions.Add(new PlayerProfession
            {
                PlayerProfileId = player2Id,
                ProfessionId = profession.Id,
                SkillLevel = 150
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/playerprofessions?playerProfileId={player2Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfessionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, link => Assert.Equal(player2Id, link.PlayerProfileId));
    }

    [Fact]
    public async Task GetAll_FilterByMinSkillLevel_ReturnsOnlyHighSkillLinks()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            SeedLink(db, player.Id, profession.Id); // SkillLevel = 300
            var lowSkillProfession = new Profession { Name = "Fishing", BenefitDescription = "Fish" };
            db.Professions.Add(lowSkillProfession);
            db.SaveChanges();
            db.PlayerProfessions.Add(new PlayerProfession
            {
                PlayerProfileId = player.Id,
                ProfessionId = lowSkillProfession.Id,
                SkillLevel = 50
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofessions?minSkillLevel=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfessionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, link => Assert.True(link.SkillLevel >= 200));
    }

    [Fact]
    public async Task GetAll_FilterByQ_ReturnsOnlyMatchingLinks()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db); // profession Name = "Alchemy"
            SeedLink(db, player.Id, profession.Id);
            var herbalism = new Profession { Name = "Herbalism", BenefitDescription = "Gather herbs" };
            db.Professions.Add(herbalism);
            db.SaveChanges();
            db.PlayerProfessions.Add(new PlayerProfession
            {
                PlayerProfileId = player.Id,
                ProfessionId = herbalism.Id,
                SkillLevel = 100
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofessions?q=Alchemy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfessionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Alchemy", list[0].Profession?.Name);
    }

    [Fact]
    public async Task Post_Returns400_WhenProfessionNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = new PlayerProfile
            {
                CharacterName = "Arthas",
                Level = 80,
                ClassType = ClassType.DeathKnight,
                LastUpdatedAt = DateTime.UtcNow
            };
            db.PlayerProfiles.Add(player);
            db.SaveChanges();
            playerId = player.Id;
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = playerId,
            ProfessionId = 99999,
            SkillLevel = 100
        };

        var response = await client.PostAsJsonAsync("/api/playerprofessions", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenLinkAlreadyExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId, professionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            SeedLink(db, player.Id, profession.Id);
            playerId = player.Id;
            professionId = profession.Id;
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = playerId,
            ProfessionId = professionId,
            SkillLevel = 200
        };

        var response = await client.PostAsJsonAsync("/api/playerprofessions", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns400_WhenRouteKeysMismatchBody()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (player, profession) = SeedPlayerAndProfession(db);
            seeded = SeedLink(db, player.Id, profession.Id);
        }

        var client = factory.CreateClient();
        // Body has different PlayerProfileId than the route
        var dto = new PlayerProfessionUpsertDto
        {
            PlayerProfileId = 99999,
            ProfessionId = seeded.ProfessionId,
            SkillLevel = 300
        };

        var response = await client.PutAsJsonAsync(
            $"/api/playerprofessions/{seeded.PlayerProfileId}/{seeded.ProfessionId}", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
