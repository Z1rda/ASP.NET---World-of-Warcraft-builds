using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/bosses/{bossId:int}/images")]
public class BossGuideImagesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public BossGuideImagesController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BossGuideImageDto>>> GetAll(int bossId)
    {
        var exists = await _db.BossGuides.AnyAsync(b => b.Id == bossId);
        if (!exists)
        {
            return NotFound();
        }

        var items = await _db.BossGuideImages
            .AsNoTracking()
            .Where(i => i.BossGuideId == bossId)
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();

        return Ok(items.Select(i => i.ToDto()));
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<BossGuideImageDto>> Upload(int bossId, [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A file is required.");
        }

        var boss = await _db.BossGuides.FirstOrDefaultAsync(b => b.Id == bossId);
        if (boss is null)
        {
            return NotFound();
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
        {
            return BadRequest("Only JPG and PNG images are allowed.");
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "bosses", bossId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var originalFileName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        var absolutePath = Path.Combine(uploadsRoot, storedFileName);
        var relativePath = $"/uploads/bosses/{bossId}/{storedFileName}";

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var image = new BossGuideImage
        {
            BossGuideId = bossId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoredFilePath = relativePath,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _db.BossGuideImages.Add(image);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { bossId }, image.ToDto());
    }

    [HttpDelete("{imageId:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int bossId, int imageId)
    {
        var image = await _db.BossGuideImages.FirstOrDefaultAsync(i => i.Id == imageId && i.BossGuideId == bossId);
        if (image is null)
        {
            return NotFound();
        }

        DeletePhysicalFile(image.StoredFilePath);
        _db.BossGuideImages.Remove(image);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void DeletePhysicalFile(string storedFilePath)
    {
        var relativePath = storedFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
        if (System.IO.File.Exists(absolutePath))
        {
            System.IO.File.Delete(absolutePath);
        }
    }
}
