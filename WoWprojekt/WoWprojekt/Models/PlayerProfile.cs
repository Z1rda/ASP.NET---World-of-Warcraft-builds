using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class PlayerProfile
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(30)]
    public string CharacterName { get; set; } = string.Empty;

    [Range(1, 80)]
    public int Level { get; set; } = 80;

    [Required]
    public ClassType ClassType { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int? GuildId { get; set; }

    [ForeignKey(nameof(GuildId))]
    public virtual Guild? Guild { get; set; }

    public virtual ICollection<TalentBuild> TalentBuilds { get; set; } = new List<TalentBuild>();
    public virtual ICollection<PlayerProfession> Professions { get; set; } = new List<PlayerProfession>();
}