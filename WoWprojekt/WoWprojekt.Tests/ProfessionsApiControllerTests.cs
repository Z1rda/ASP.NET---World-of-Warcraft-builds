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

public class ProfessionsApiControllerTests
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

    private static Profession SeedProfession(ApplicationDbContext db, string name = "Alchemy")
    {
        var profession = new Profession { Name = name, BenefitDescription = "Craft potions" };
        db.Professions.Add(profession);
        db.SaveChanges();
        return profession;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SeedProfession(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/professions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ProfessionDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetById_Returns200_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Profession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedProfession(db);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/professions/{seeded.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProfessionDto>(JsonOptions);
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
        var response = await client.GetAsync("/api/professions/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Creates_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new ProfessionUpsertDto { Name = "Blacksmithing", BenefitDescription = "Craft weapons and armor" };

        var response = await client.PostAsJsonAsync("/api/professions", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProfessionDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Blacksmithing", created.Name);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WithoutName()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var content = new StringContent(
            """{"Name":null,"BenefitDescription":"Some benefit"}""",
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/professions", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Updates_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Profession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedProfession(db);
        }

        var client = factory.CreateClient();
        var dto = new ProfessionUpsertDto { Name = "Updated Alchemy", BenefitDescription = "Updated description" };

        var response = await client.PutAsJsonAsync($"/api/professions/{seeded.Id}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var dto = new ProfessionUpsertDto { Name = "Ghost Profession", BenefitDescription = "None" };

        var response = await client.PutAsJsonAsync("/api/professions/99999", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        Profession seeded;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seeded = SeedProfession(db);
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/professions/{seeded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentId()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.DeleteAsync("/api/professions/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
