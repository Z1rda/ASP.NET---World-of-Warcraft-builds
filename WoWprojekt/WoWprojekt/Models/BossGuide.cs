using System.ComponentModel.DataAnnotations;

namespace WoWprojekt.Models;

public class BossGuide
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string BossName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Tactics { get; set; } = string.Empty;

    [Range(1, 10)]
    public int DifficultyRating { get; set; }

    public int RaidGuideId { get; set; }
    public RaidGuide? RaidGuide { get; set; }
}