using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WoWprojekt.Models;

namespace WoWprojekt.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BossGuide> BossGuides => Set<BossGuide>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<PlayerProfession> PlayerProfessions => Set<PlayerProfession>();
    public DbSet<Profession> Professions => Set<Profession>();
    public DbSet<RaidGuide> RaidGuides => Set<RaidGuide>();
    public DbSet<TalentBuild> TalentBuilds => Set<TalentBuild>();
    public DbSet<TalentBuildAttachment> TalentBuildAttachments => Set<TalentBuildAttachment>();
    public DbSet<BossGuideImage> BossGuideImages => Set<BossGuideImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerProfession>()
            .HasKey(pp => new { pp.PlayerProfileId, pp.ProfessionId });

        modelBuilder.Entity<PlayerProfession>()
            .HasOne(pp => pp.PlayerProfile)
            .WithMany(p => p.Professions)
            .HasForeignKey(pp => pp.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerProfession>()
            .HasOne(pp => pp.Profession)
            .WithMany(p => p.Players)
            .HasForeignKey(pp => pp.ProfessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BossGuide>()
            .HasOne(b => b.RaidGuide)
            .WithMany(r => r.Bosses)
            .HasForeignKey(b => b.RaidGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TalentBuild>()
            .HasOne(t => t.PlayerProfile)
            .WithMany(p => p.TalentBuilds)
            .HasForeignKey(t => t.PlayerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TalentBuildAttachment>()
            .HasOne(a => a.TalentBuild)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TalentBuildId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BossGuideImage>()
            .HasOne(i => i.BossGuide)
            .WithMany(b => b.Images)
            .HasForeignKey(i => i.BossGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerProfile>()
            .HasOne(p => p.Guild)
            .WithMany(g => g.Members)
            .HasForeignKey(p => p.GuildId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
