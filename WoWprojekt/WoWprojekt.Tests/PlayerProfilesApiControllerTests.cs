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

public class PlayerProfilesApiControllerTests
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

    private static PlayerProfile SeedPlayer(ApplicationDbContext db, string name = "Arthas")
    {
        var player = new PlayerProfile
        {
            CharacterName = name,
            Level = 80,
            ClassType = ClassType.DeathKnight,
            LastUpdatedAt = DateTime.UtcNow
        };
        db.PlayerProfiles.Add(player);
        db.SaveChanges();
        return player;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfile seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedPlayer(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/playerprofiles/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PlayerProfileDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seeded.CharacterName, dto.CharacterName);
        Assert.Equal(seeded.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Thrall",
            Level = 80,
            ClassType = ClassType.Shaman
        };

        var response = await client.PostAsJsonAsync("/api/playerprofiles", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PlayerProfileDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Thrall", created.CharacterName);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WithoutCharacterName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var content = new StringContent(
            """{"CharacterName":null,"Level":80,"ClassType":0}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/playerprofiles", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfile seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedPlayer(db);
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Updated Arthas",
            Level = 80,
            ClassType = ClassType.Paladin
        };

        var response = await client.PutAsJsonAsync($"/api/playerprofiles/{seeded.Id}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Ghost Player",
            Level = 1,
            ClassType = ClassType.Warrior
        };

        var response = await client.PutAsJsonAsync("/api/playerprofiles/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfile seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedPlayer(db);
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/playerprofiles/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/playerprofiles/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FilterByQ_ReturnsOnlyMatchingPlayers()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db, "Arthas");
            SeedPlayer(db, "Thrall");
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles?q=Thrall");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Thrall", list[0].CharacterName);
    }

    [Fact]
    public async Task GetAll_FilterByClassType_ReturnsOnlyMatchingPlayers()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db, "Arthas"); // ClassType.DeathKnight
            db.PlayerProfiles.Add(new PlayerProfile
            {
                CharacterName = "Thrall",
                Level = 80,
                ClassType = ClassType.Shaman,
                LastUpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/playerprofiles?classType={(int)ClassType.Shaman}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, p => Assert.Equal(ClassType.Shaman, p.ClassType));
    }

    [Fact]
    public async Task GetAll_FilterByMinLevel_ReturnsOnlyHighLevelPlayers()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db, "Arthas"); // Level 80
            db.PlayerProfiles.Add(new PlayerProfile
            {
                CharacterName = "Newbie",
                Level = 10,
                ClassType = ClassType.Warrior,
                LastUpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles?minLevel=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, p => Assert.True(p.Level >= 50));
    }

    [Fact]
    public async Task GetAll_FilterByHasGuild_True_ReturnsOnlyGuildMembers()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var guild = new Guild { Name = "Test Guild", Realm = "Stormrage", CreatedAt = DateTime.UtcNow };
            db.Guilds.Add(guild);
            db.SaveChanges();
            db.PlayerProfiles.Add(new PlayerProfile
            {
                CharacterName = "GuildMember",
                Level = 80,
                ClassType = ClassType.Warrior,
                LastUpdatedAt = DateTime.UtcNow,
                GuildId = guild.Id
            });
            SeedPlayer(db, "Loner"); // no guild
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles?hasGuild=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, p => Assert.NotNull(p.GuildId));
    }

    [Fact]
    public async Task GetAll_FilterByHasGuild_False_ReturnsOnlyGuildless()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var guild = new Guild { Name = "Test Guild", Realm = "Stormrage", CreatedAt = DateTime.UtcNow };
            db.Guilds.Add(guild);
            db.SaveChanges();
            db.PlayerProfiles.Add(new PlayerProfile
            {
                CharacterName = "GuildMember",
                Level = 80,
                ClassType = ClassType.Warrior,
                LastUpdatedAt = DateTime.UtcNow,
                GuildId = guild.Id
            });
            SeedPlayer(db, "Loner");
            db.SaveChanges();
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/playerprofiles?hasGuild=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<PlayerProfileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.All(list, p => Assert.Null(p.GuildId));
    }

    [Fact]
    public async Task Post_Returns400_WhenCharacterNameAlreadyExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db, "Arthas");
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Arthas",
            Level = 80,
            ClassType = ClassType.Paladin
        };

        var response = await client.PostAsJsonAsync("/api/playerprofiles", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenGuildIdNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Arthas",
            Level = 80,
            ClassType = ClassType.DeathKnight,
            GuildId = 99999
        };

        var response = await client.PostAsJsonAsync("/api/playerprofiles", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns400_WhenCharacterNameAlreadyTakenByOtherPlayer()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfile player2;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedPlayer(db, "Arthas");
            player2 = SeedPlayer(db, "Thrall");
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = "Arthas", // already taken
            Level = 80,
            ClassType = ClassType.Shaman
        };

        var response = await client.PutAsJsonAsync($"/api/playerprofiles/{player2.Id}", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns400_WhenGuildIdNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        PlayerProfile seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedPlayer(db);
        }

        var client = factory.CreateClient();
        var dto = new PlayerProfileUpsertDto
        {
            CharacterName = seeded.CharacterName,
            Level = 80,
            ClassType = ClassType.DeathKnight,
            GuildId = 99999
        };

        var response = await client.PutAsJsonAsync($"/api/playerprofiles/{seeded.Id}", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
