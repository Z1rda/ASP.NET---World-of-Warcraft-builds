using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class BossGuideImage
{
    public int Id { get; set; }

    [Required]
    public int BossGuideId { get; set; }

    [ForeignKey(nameof(BossGuideId))]
    public virtual BossGuide? BossGuide { get; set; }

    [Required]
    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string StoredFilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }
}
