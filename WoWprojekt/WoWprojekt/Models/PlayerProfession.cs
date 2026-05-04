using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WoWprojekt.Models;

public class PlayerProfession
{
    public int PlayerProfileId { get; set; }

    [ForeignKey(nameof(PlayerProfileId))]
    public virtual PlayerProfile? PlayerProfile { get; set; }

    public int ProfessionId { get; set; }

    [ForeignKey(nameof(ProfessionId))]
    public virtual Profession? Profession { get; set; }

    [Range(1, 450)]
    public int SkillLevel { get; set; }
}