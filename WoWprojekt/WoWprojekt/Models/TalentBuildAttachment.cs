using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class TalentBuildAttachment
{
    public int Id { get; set; }

    public int TalentBuildId { get; set; }

    [ForeignKey(nameof(TalentBuildId))]
    public virtual TalentBuild? TalentBuild { get; set; }

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}