using System.ComponentModel.DataAnnotations;

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
    public PlayerProfile? PlayerProfile { get; set; }
}