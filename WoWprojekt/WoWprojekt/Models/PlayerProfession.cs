namespace WoWprojekt.Models;

public class PlayerProfession
{
    public int PlayerProfileId { get; set; }
    public PlayerProfile? PlayerProfile { get; set; }

    public int ProfessionId { get; set; }
    public Profession? Profession { get; set; }

    public int SkillLevel { get; set; }
}