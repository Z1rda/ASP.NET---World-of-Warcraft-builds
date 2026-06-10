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

public class GuildsApiControllerTests
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

    private static Guild SeedGuild(ApplicationDbContext db, string name = "Test Guild")
    {
        var guild = new Guild { Name = name, Realm = "Stormrage" };
        db.Guilds.Add(guild);
        db.SaveChanges();
        return guild;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedGuild(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/guilds");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<GuildDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Guild seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedGuild(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/guilds/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<GuildDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.Equal(seeded.Name, dto.Name);
        Assert.Equal(seeded.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/guilds/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new GuildUpsertDto { Name = "Frost Knights", Realm = "Icecrown" };

        var response = await client.PostAsJsonAsync("/api/guilds", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<GuildDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Frost Knights", created.Name);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WithoutName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var content = new StringContent(
            """{"Name":null,"Realm":"Icecrown"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/guilds", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Guild seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedGuild(db);
        }

        var client = factory.CreateClient();
        var dto = new GuildUpsertDto { Name = "Updated Guild", Realm = "Silvermoon" };

        var response = await client.PutAsJsonAsync($"/api/guilds/{seeded.Id}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new GuildUpsertDto { Name = "Ghost Guild", Realm = "Nowhere" };

        var response = await client.PutAsJsonAsync("/api/guilds/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Guild seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedGuild(db);
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/guilds/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/guilds/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
