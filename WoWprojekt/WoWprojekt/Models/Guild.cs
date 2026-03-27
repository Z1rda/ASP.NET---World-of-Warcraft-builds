using System.ComponentModel.DataAnnotations;

namespace WoWprojekt.Models;

public class Guild
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(60)]
    public string Realm { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlayerProfile> Members { get; set; } = new List<PlayerProfile>();
}