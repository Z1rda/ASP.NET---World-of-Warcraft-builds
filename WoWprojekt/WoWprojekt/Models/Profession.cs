using System.ComponentModel.DataAnnotations;

namespace WoWprojekt.Models;

public class Profession
{
    public int Id { get; set; }

    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string BenefitDescription { get; set; } = string.Empty;

    public ICollection<PlayerProfession> Players { get; set; } = new List<PlayerProfession>();
}