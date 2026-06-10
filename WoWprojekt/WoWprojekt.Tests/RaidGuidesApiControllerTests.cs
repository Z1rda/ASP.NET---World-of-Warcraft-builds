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

public class RaidGuidesApiControllerTests
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

    private static RaidGuide SeedRaid(ApplicationDbContext db, string name = "Naxxramas")
    {
        var raid = new RaidGuide { RaidName = name, PreparationNotes = "Bring consumables" };
        db.RaidGuides.Add(raid);
        db.SaveChanges();
        return raid;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedRaid(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/raidguides");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<RaidGuideDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        RaidGuide seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedRaid(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/raidguides/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<RaidGuideDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seeded.RaidName, dto.RaidName);
        Assert.Equal(seeded.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/raidguides/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new RaidGuideUpsertDto { RaidName = "Ulduar", PreparationNotes = "Hard mode available" };

        var response = await client.PostAsJsonAsync("/api/raidguides", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RaidGuideDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Ulduar", created.RaidName);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WithoutRaidName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var content = new StringContent(
            """{"RaidName":null,"PreparationNotes":"Some notes"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/raidguides", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        RaidGuide seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedRaid(db);
        }

        var client = factory.CreateClient();
        var dto = new RaidGuideUpsertDto { RaidName = "Updated Naxxramas", PreparationNotes = "Updated notes" };

        var response = await client.PutAsJsonAsync($"/api/raidguides/{seeded.Id}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new RaidGuideUpsertDto { RaidName = "Ghost Raid", PreparationNotes = "None" };

        var response = await client.PutAsJsonAsync("/api/raidguides/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        RaidGuide seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedRaid(db);
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/raidguides/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/raidguides/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
