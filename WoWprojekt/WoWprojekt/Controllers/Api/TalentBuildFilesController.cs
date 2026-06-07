using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WoWprojekt.Api;
using WoWprojekt.Data;
using WoWprojekt.Models;

namespace WoWprojekt.Controllers.Api;

[ApiController]
[Route("api/talentbuilds/{talentBuildId:int}/files")]
public class TalentBuildFilesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public TalentBuildFilesController(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TalentBuildFileDto>>> GetAll(int talentBuildId)
    {
        var exists = await _db.TalentBuilds.AnyAsync(build => build.Id == talentBuildId);
        if (!exists)
        {
            return NotFound();
        }

        var files = await _db.TalentBuildAttachments
            .AsNoTracking()
            .Where(file => file.TalentBuildId == talentBuildId)
            .OrderBy(file => file.UploadedAt)
            .ToListAsync();

        return Ok(files.Select(file => file.ToDto()));
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<TalentBuildFileDto>> Upload(int talentBuildId, [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A file is required.");
        }

        var build = await _db.TalentBuilds.FirstOrDefaultAsync(item => item.Id == talentBuildId);
        if (build is null)
        {
            return NotFound();
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "talentbuilds", talentBuildId.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var originalFileName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        var absolutePath = Path.Combine(uploadsRoot, storedFileName);
        var relativePath = $"/uploads/talentbuilds/{talentBuildId}/{storedFileName}";

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new TalentBuildAttachment
        {
            TalentBuildId = talentBuildId,
            OriginalFileName = originalFileName,
            StoredFileName = storedFileName,
            StoredFilePath = relativePath,
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        _db.TalentBuildAttachments.Add(attachment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { talentBuildId }, attachment.ToDto());
    }

    [HttpDelete("{fileId:int}")]
    public async Task<IActionResult> Delete(int talentBuildId, int fileId)
    {
        var attachment = await _db.TalentBuildAttachments.FirstOrDefaultAsync(file => file.Id == fileId && file.TalentBuildId == talentBuildId);
        if (attachment is null)
        {
            return NotFound();
        }

        DeletePhysicalFile(attachment.StoredFilePath);

        _db.TalentBuildAttachments.Remove(attachment);
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