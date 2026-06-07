using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class TalentBuild
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string BuildName { get; set; } = string.Empty;

    [StringLength(255)]
    public string TalentCode { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

    public int PlayerProfileId { get; set; }

    [ForeignKey(nameof(PlayerProfileId))]
    public virtual PlayerProfile? PlayerProfile { get; set; }

    public virtual ICollection<TalentBuildAttachment> Attachments { get; set; } = new List<TalentBuildAttachment>();
}