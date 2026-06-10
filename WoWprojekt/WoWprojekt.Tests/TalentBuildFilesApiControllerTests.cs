using System.Net;
using System.Net.Http.Json;
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

// TalentBuildFilesController has no GetById or Put — it only exposes
// GET (all files for a build), POST (upload), and DELETE (by fileId).
public class TalentBuildFilesApiControllerTests
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

    private static TalentBuild SeedBuild(ApplicationDbContext db)
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

        var build = new TalentBuild
        {
            BuildName = "Frost DK",
            TalentCode = "ABC123",
            Description = "Standard frost build",
            PublishedAt = DateTime.UtcNow,
            PlayerProfileId = player.Id
        };
        db.TalentBuilds.Add(build);
        db.SaveChanges();
        return build;
    }

    private static TalentBuildAttachment SeedAttachment(ApplicationDbContext db, int talentBuildId)
    {
        var attachment = new TalentBuildAttachment
        {
            TalentBuildId = talentBuildId,
            OriginalFileName = "guide.txt",
            StoredFileName = "guide_stored.txt",
            StoredFilePath = "/uploads/talentbuilds/1/guide_stored.txt",
            ContentType = "text/plain",
            FileSize = 100,
            UploadedAt = DateTime.UtcNow
        };
        db.TalentBuildAttachments.Add(attachment);
        db.SaveChanges();
        return attachment;
    }

    [Fact]
    public async Task GetAll_Returns200AndList()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var build = SeedBuild(db);
            buildId = build.Id;
            SeedAttachment(db, buildId);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/talentbuilds/{buildId}/files");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<TalentBuildFileDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetAll_Returns404_WhenBuildDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/talentbuilds/99999/files");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Upload_Returns201()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            buildId = SeedBuild(db).Id;
        }

        var client = factory.CreateClient();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("talent build data"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "talents.txt");

        var response = await client.PostAsync($"/api/talentbuilds/{buildId}/files", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TalentBuildFileDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(buildId, created.TalentBuildId);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Post_Returns400_WhenFileIsMissing()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            buildId = SeedBuild(db).Id;
        }

        var client = factory.CreateClient();
        // Send empty multipart — no file part
        var form = new MultipartFormDataContent();

        var response = await client.PostAsync($"/api/talentbuilds/{buildId}/files", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns404_WhenBuildDoesNotExist()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("data"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.txt");

        var response = await client.PostAsync("/api/talentbuilds/99999/files", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Removes_Returns204()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId, fileId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var build = SeedBuild(db);
            buildId = build.Id;
            fileId = SeedAttachment(db, buildId).Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/talentbuilds/{buildId}/files/{fileId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_ForNonExistentFile()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int buildId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            buildId = SeedBuild(db).Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/talentbuilds/{buildId}/files/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
