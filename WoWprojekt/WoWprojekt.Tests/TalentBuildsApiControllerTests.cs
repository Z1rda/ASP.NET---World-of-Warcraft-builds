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

public class TalentBuildsApiControllerTests
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

    private static PlayerProfile SeedPlayer(ApplicationDbContext db)
    {
        var player = new PlayerProfile
        {
            CharacterName = "Arthas",
            Level = 80,
            ClassType = ClassType.DeathKnight,
            LastUpdatedAt = DateTime.UtcNow
        };
        db.PlayerProfiles.Add(player);
        db.SaveChanges();
        return player;
    }

    private static TalentBuild SeedBuild(ApplicationDbContext db, int playerProfileId)
    {
        var build = new TalentBuild
        {
            BuildName = "Frost DK",
            TalentCode = "ABC123",
            Description = "Standard frost build",
            PublishedAt = DateTime.UtcNow,
            PlayerProfileId = playerProfileId
        };
        db.TalentBuilds.Add(build);
        db.SaveChanges();
        return build;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            SeedBuild(db, player.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/talentbuilds");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TalentBuildDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        TalentBuild seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            seeded = SeedBuild(db, player.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/talentbuilds/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<TalentBuildDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seeded.BuildName, dto.BuildName);
        Assert.Equal(seeded.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/talentbuilds/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            playerId = SeedPlayer(db).Id;
        }

        var client = factory.CreateClient();
        var dto = new TalentBuildUpsertDto
        {
            BuildName = "Unholy Burst",
            TalentCode = "XYZ789",
            Description = "PvP burst build",
            PlayerProfileId = playerId
        };

        var response = await client.PostAsJsonAsync("/api/talentbuilds", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TalentBuildDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Unholy Burst", created.BuildName);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WithoutBuildName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            playerId = SeedPlayer(db).Id;
        }

        var client = factory.CreateClient();
        var content = new StringContent(
            $$$"""{"BuildName":null,"TalentCode":"ABC","Description":"desc","PlayerProfileId":{{{playerId}}}}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/talentbuilds", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId;
        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            playerId = player.Id;
            buildId = SeedBuild(db, playerId).Id;
        }

        var client = factory.CreateClient();
        var dto = new TalentBuildUpsertDto
        {
            BuildName = "Updated Frost DK",
            TalentCode = "UPD999",
            Description = "Updated description",
            PlayerProfileId = playerId
        };

        var response = await client.PutAsJsonAsync($"/api/talentbuilds/{buildId}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int playerId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            playerId = SeedPlayer(db).Id;
        }

        var client = factory.CreateClient();
        var dto = new TalentBuildUpsertDto
        {
            BuildName = "Ghost Build",
            TalentCode = "GHOST",
            Description = "None",
            PlayerProfileId = playerId
        };

        var response = await client.PutAsJsonAsync("/api/talentbuilds/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            buildId = SeedBuild(db, player.Id).Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/talentbuilds/{buildId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/talentbuilds/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FilterByQ_ReturnsOnlyMatchingBuilds()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            SeedBuild(db, player.Id); // BuildName "Frost DK"
            db.TalentBuilds.Add(new TalentBuild
            {
                BuildName = "Holy Paladin",
                TalentCode = "HOLY999",
                Description = "Healing build",
                PublishedAt = DateTime.UtcNow,
                PlayerProfileId = player.Id
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/talentbuilds?q=Holy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TalentBuildDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Holy Paladin", list[0].BuildName);
    }

    [Fact]
    public async Task GetAll_FilterByPlayerProfileId_ReturnsOnlyThatPlayersBuilds()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int player2Id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player1 = SeedPlayer(db);
            SeedBuild(db, player1.Id);
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
            db.TalentBuilds.Add(new TalentBuild
            {
                BuildName = "Enhancement",
                TalentCode = "ENH001",
                Description = "Melee shaman",
                PublishedAt = DateTime.UtcNow,
                PlayerProfileId = player2Id
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/talentbuilds?playerProfileId={player2Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TalentBuildDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, b => Assert.Equal(player2Id, b.PlayerProfileId));
    }

    [Fact]
    public async Task GetAll_FilterByClassType_ReturnsOnlyMatchingBuilds()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player1 = SeedPlayer(db); // DeathKnight
            SeedBuild(db, player1.Id);
            var player2 = new PlayerProfile
            {
                CharacterName = "Thrall",
                Level = 80,
                ClassType = ClassType.Shaman,
                LastUpdatedAt = DateTime.UtcNow
            };
            db.PlayerProfiles.Add(player2);
            db.SaveChanges();
            db.TalentBuilds.Add(new TalentBuild
            {
                BuildName = "Enhancement",
                TalentCode = "ENH001",
                Description = "Melee shaman",
                PublishedAt = DateTime.UtcNow,
                PlayerProfileId = player2.Id
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/talentbuilds?classType={(int)ClassType.DeathKnight}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TalentBuildDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.All(list, b => Assert.NotNull(b.PlayerProfile));
    }

    [Fact]
    public async Task Post_Returns400_WhenPlayerProfileIdNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new TalentBuildUpsertDto
        {
            BuildName = "Ghost Build",
            TalentCode = "GHOST",
            Description = "No player",
            PlayerProfileId = 99999
        };

        var response = await client.PostAsJsonAsync("/api/talentbuilds", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns400_WhenPlayerProfileIdNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = SeedPlayer(db);
            buildId = SeedBuild(db, player.Id).Id;
        }

        var client = factory.CreateClient();
        var dto = new TalentBuildUpsertDto
        {
            BuildName = "Updated Build",
            TalentCode = "UPD001",
            Description = "Updated",
            PlayerProfileId = 99999
        };

        var response = await client.PutAsJsonAsync($"/api/talentbuilds/{buildId}", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
