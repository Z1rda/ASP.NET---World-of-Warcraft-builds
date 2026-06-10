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

public class BossGuideImagesControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static WebApplicationFactory<Program> CreateFactory(string dbName)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseWebRoot(Path.GetTempPath());

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

    private static (RaidGuide raid, BossGuide boss) SeedRaidAndBoss(ApplicationDbContext db)
    {
        var raid = new RaidGuide { RaidName = "Test Raid", PreparationNotes = "Notes" };
        db.RaidGuides.Add(raid);
        db.SaveChanges();

        var boss = new BossGuide
        {
            BossName = "Test Boss",
            Tactics = "Stand in the fire",
            BossImageUrl = "https://example.com/boss.jpg",
            DifficultyRating = 5,
            RaidGuideId = raid.Id
        };
        db.BossGuides.Add(boss);
        db.SaveChanges();

        return (raid, boss);
    }

    private static BossGuideImage SeedImage(ApplicationDbContext db, int bossId)
    {
        var image = new BossGuideImage
        {
            BossGuideId = bossId,
            OriginalFileName = "test.jpg",
            StoredFileName = "abc123.jpg",
            StoredFilePath = $"/uploads/bosses/{bossId}/abc123.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            UploadedAt = DateTime.UtcNow
        };
        db.BossGuideImages.Add(image);
        db.SaveChanges();
        return image;
    }

    // GET /api/bosses/{bossId}/images — returns 200 and list when boss exists
    [Fact]
    public async Task GetAll_Returns200AndList_WhenBossExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
            SeedImage(db, bossId);
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/bosses/{bossId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<BossGuideImageDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list);
        Assert.All(list, item => Assert.Equal(bossId, item.BossGuideId));
    }

    // GET /api/bosses/{bossId}/images — returns 200 and empty list when boss exists but has no images
    [Fact]
    public async Task GetAll_Returns200AndEmptyList_WhenBossHasNoImages()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
        }

        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/bosses/{bossId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<BossGuideImageDto>>(JsonOptions);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    // GET /api/bosses/{bossId}/images — returns 404 when boss does not exist
    [Fact]
    public async Task GetAll_Returns404_WhenBossNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/bosses/99999/images");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/bosses/{bossId}/images/{imageId} — returns 404 when image does not exist
    [Fact]
    public async Task Delete_Returns404_WhenImageNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/bosses/{bossId}/images/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // DELETE /api/bosses/{bossId}/images/{imageId} — returns 204 when image exists (no physical file in test)
    [Fact]
    public async Task Delete_Returns204_WhenImageExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        int imageId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
            imageId = SeedImage(db, bossId).Id;
        }

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/bosses/{bossId}/images/{imageId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // DELETE /api/bosses/{bossId}/images/{imageId} — returns 404 when imageId belongs to different boss
    [Fact]
    public async Task Delete_Returns404_WhenImageBelongsToDifferentBoss()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int boss1Id;
        int boss2Id;
        int imageId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (raid, boss1) = SeedRaidAndBoss(db);
            boss1Id = boss1.Id;
            imageId = SeedImage(db, boss1Id).Id;

            var boss2 = new BossGuide
            {
                BossName = "Other Boss",
                Tactics = "Other tactics",
                BossImageUrl = "https://example.com/other.jpg",
                DifficultyRating = 3,
                RaidGuideId = raid.Id
            };
            db.BossGuides.Add(boss2);
            db.SaveChanges();
            boss2Id = boss2.Id;
        }

        var client = factory.CreateClient();
        // imageId belongs to boss1, but we query it under boss2
        var response = await client.DeleteAsync($"/api/bosses/{boss2Id}/images/{imageId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // POST /api/bosses/{bossId}/images — returns 201 for valid JPG when boss exists
    [Fact]
    public async Task Upload_Returns201_WhenValidJpgAndBossExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
        }

        var client = factory.CreateClient();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "screenshot.jpg");

        var response = await client.PostAsync($"/api/bosses/{bossId}/images", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BossGuideImageDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(bossId, created.BossGuideId);
        Assert.True(created.Id > 0);
    }

    // POST /api/bosses/{bossId}/images — returns 404 when boss does not exist
    [Fact]
    public async Task Upload_Returns404_WhenBossNotExists()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        var client = factory.CreateClient();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "screenshot.jpg");

        var response = await client.PostAsync("/api/bosses/99999/images", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // POST /api/bosses/{bossId}/images — returns 400 for invalid extension (.gif)
    [Fact]
    public async Task Upload_Returns400_WhenInvalidExtension()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
        }

        var client = factory.CreateClient();
        var fileContent = new ByteArrayContent(new byte[] { 0x47, 0x49, 0x46, 0x38 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "animation.gif");

        var response = await client.PostAsync($"/api/bosses/{bossId}/images", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // POST /api/bosses/{bossId}/images — returns 400 when no file is attached
    [Fact]
    public async Task Upload_Returns400_WhenFileNotAttached()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var factory = CreateFactory(dbName);

        int bossId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (_, boss) = SeedRaidAndBoss(db);
            bossId = boss.Id;
        }

        var client = factory.CreateClient();
        var form = new MultipartFormDataContent();

        var response = await client.PostAsync($"/api/bosses/{bossId}/images", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
