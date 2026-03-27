using System.ComponentModel.DataAnnotations;

namespace WoWprojekt.Models;

public class RaidGuide
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string RaidName { get; set; } = string.Empty;

    [StringLength(500)]
    public string PreparationNotes { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BossGuide> Bosses { get; set; } = new List<BossGuide>();
}