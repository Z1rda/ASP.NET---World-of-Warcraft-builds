using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;
using Xunit;

namespace WoWprojekt.Tests;

public class BossGuidesApiControllerTests
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
                    // In Testing environment, MySQL is never registered (see Program.cs guard),
                    // so we only need to add the InMemory DbContext here.
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    // Replace authentication with a test scheme that always authenticates as admin
                    // so that POST / PUT / DELETE pass the RequestMethodAuthorizationFilter.
                    services.AddAuthentication(opts =>
                    {
                        opts.DefaultAuthenticateScheme = "Test";
                        opts.DefaultChallengeScheme = "Test";
                        opts.DefaultForbidScheme = "Test";
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                });
            });
    }

    private static RaidGuide SeedRaid(ApplicationDbContext db)
    {
        var raid = new RaidGuide { RaidName = "Test Raid", PreparationNotes = "Notes" };
        db.RaidGuides.Add(raid);
        db.SaveChanges();
        return raid;
    }

    private static BossGuide SeedBoss(ApplicationDbContext db, int raidGuideId)
    {
        var boss = new BossGuide
        {
            BossName = "Test Boss",
            Tactics = "Stand in the fire",
            BossImageUrl = "https://example.com/boss.jpg",
            DifficultyRating = 5,
            RaidGuideId = raidGuideId
        };
        db.BossGuides.Add(boss);
        db.SaveChanges();
        return boss;
    }

    // GET /api/boss-guides — returns 200 and a list
    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var raid = SeedRaid(db);
            SeedBoss(db, raid.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/boss-guides");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<BossGuideDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    // GET /api/boss-guides/{id} — returns 200 and record when it exists
    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        BossGuide seededBoss;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var raid = SeedRaid(db);
            seededBoss = SeedBoss(db, raid.Id);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/boss-guides/{seededBoss.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<BossGuideDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seededBoss.BossName, dto.BossName);
        Assert.Equal(seededBoss.Id, dto.Id);
    }

    // GET /api/boss-guides/{id} — returns 404 when record does not exist
    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/boss-guides/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // POST /api/boss-guides — creates record and returns 201
    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int raidId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            raidId = SeedRaid(db).Id;
        }

        var client = factory.CreateClient();
        var dto = new BossGuideUpsertDto
        {
            BossName = "Kel'Thuzad",
            Tactics = "Interrupt Frost Bolt Volley",
            BossImageUrl = "https://example.com/kelthuzad.jpg",
            DifficultyRating = 9,
            RaidGuideId = raidId
        };

        var response = await client.PostAsJsonAsync("/api/boss-guides", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BossGuideDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Kel'Thuzad", created.BossName);
        Assert.True(created.Id > 0);
    }

    // POST /api/boss-guides — returns 400 for invalid model (BossName missing)
    [Fact]
    public async Task Post_Returns400_WithoutBossName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        // BossName explicitly null triggers [Required] failure
        var content = new StringContent(
            """{"BossName":null,"BossImageUrl":"https://example.com/img.jpg","DifficultyRating":5,"RaidGuideId":1}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/boss-guides", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // PUT /api/boss-guides/{id} — updates existing record and returns 204
    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int raidId;
        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var raid = SeedRaid(db);
            raidId = raid.Id;
            bossId = SeedBoss(db, raidId).Id;
        }

        var client = factory.CreateClient();
        var dto = new BossGuideUpsertDto
        {
            BossName = "Updated Boss",
            Tactics = "Updated tactics",
            BossImageUrl = "https://example.com/updated.jpg",
            DifficultyRating = 8,
            RaidGuideId = raidId
        };

        var response = await client.PutAsJsonAsync($"/api/boss-guides/{bossId}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // PUT /api/boss-guides/{id} — returns 404 for non-existent id
    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int raidId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            raidId = SeedRaid(db).Id;
        }

        var client = factory.CreateClient();
        var dto = new BossGuideUpsertDto
        {
            BossName = "Ghost Boss",
            Tactics = "None",
            BossImageUrl = "https://example.com/ghost.jpg",
            DifficultyRating = 5,
            RaidGuideId = raidId
        };

        var response = await client.PutAsJsonAsync("/api/boss-guides/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/boss-guides/{id} — deletes record and returns 204
    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var raid = SeedRaid(db);
            bossId = SeedBoss(db, raid.Id).Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/boss-guides/{bossId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // DELETE /api/boss-guides/{id} — returns 404 for non-existent id
    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/boss-guides/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Test auth handler — every request is authenticated as an admin user so that
/// the RequestMethodAuthorizationFilter allows POST / PUT / DELETE.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testadmin"),
            new Claim(ClaimTypes.NameIdentifier, "test-admin-id"),
            new Claim(ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
