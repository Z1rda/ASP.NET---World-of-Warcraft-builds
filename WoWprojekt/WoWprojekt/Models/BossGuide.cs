using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class BossGuide
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string BossName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Tactics { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Url]
    public string BossImageUrl { get; set; } = string.Empty;

    [Range(1, 10)]
    public int DifficultyRating { get; set; }

    [Required]
    public int RaidGuideId { get; set; }

    [ForeignKey(nameof(RaidGuideId))]
    public virtual RaidGuide? RaidGuide { get; set; }
    public virtual ICollection<BossGuideImage> Images { get; set; } = new List<BossGuideImage>();
}