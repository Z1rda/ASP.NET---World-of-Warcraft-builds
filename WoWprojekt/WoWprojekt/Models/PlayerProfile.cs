using System.ComponentModel.DataAnnotations;

namespace WoWprojekt.Models;

public class PlayerProfile
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string CharacterName { get; set; } = string.Empty;

    [Range(1, 80)]
    public int Level { get; set; } = 80;

    public ClassType ClassType { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int? GuildId { get; set; } //uzima se id iz Guild klase, nullable jer igrač možda nije u guildu i sprema kao int
    public Guild? Guild { get; set; } //isto kao gore samo uzima cijeli guild umjesto samo id-a

    public ICollection<TalentBuild> TalentBuilds { get; set; } = new List<TalentBuild>();
    public ICollection<PlayerProfession> Professions { get; set; } = new List<PlayerProfession>();
}