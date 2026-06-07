using WoWprojekt.Models;

namespace WoWprojekt.Api;

public sealed class PlayerProfileDto
{
    public int Id { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Level { get; set; }
    public ClassType ClassType { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public int? GuildId { get; set; }
    public GuildSummaryDto? Guild { get; set; }
    public IReadOnlyList<TalentBuildSummaryDto> TalentBuilds { get; set; } = Array.Empty<TalentBuildSummaryDto>();
    public IReadOnlyList<PlayerProfessionDto> Professions { get; set; } = Array.Empty<PlayerProfessionDto>();
}

public sealed class PlayerProfileSummaryDto
{
    public int Id { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int Level { get; set; }
    public ClassType ClassType { get; set; }
}

public sealed class PlayerProfileUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string CharacterName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, 80)]
    public int Level { get; set; } = 80;

    [System.ComponentModel.DataAnnotations.Required]
    public ClassType ClassType { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public int? GuildId { get; set; }
}

public sealed class GuildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<PlayerProfileSummaryDto> Members { get; set; } = Array.Empty<PlayerProfileSummaryDto>();
}

public sealed class GuildSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public sealed class GuildUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string Realm { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
}

public sealed class ProfessionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BenefitDescription { get; set; } = string.Empty;
    public IReadOnlyList<PlayerProfessionSummaryDto> Players { get; set; } = Array.Empty<PlayerProfessionSummaryDto>();
}

public sealed class ProfessionSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BenefitDescription { get; set; } = string.Empty;
}

public sealed class ProfessionUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string BenefitDescription { get; set; } = string.Empty;
}

public sealed class RaidGuideDto
{
    public int Id { get; set; }
    public string RaidName { get; set; } = string.Empty;
    public string PreparationNotes { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<BossGuideSummaryDto> Bosses { get; set; } = Array.Empty<BossGuideSummaryDto>();
}

public sealed class RaidGuideSummaryDto
{
    public int Id { get; set; }
    public string RaidName { get; set; } = string.Empty;
    public int BossCount { get; set; }
}

public sealed class RaidGuideUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string RaidName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string PreparationNotes { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }
}

public sealed class BossGuideDto
{
    public int Id { get; set; }
    public string BossName { get; set; } = string.Empty;
    public string Tactics { get; set; } = string.Empty;
    public string BossImageUrl { get; set; } = string.Empty;
    public int DifficultyRating { get; set; }
    public int RaidGuideId { get; set; }
    public RaidGuideSummaryDto? RaidGuide { get; set; }
}

public sealed class BossGuideSummaryDto
{
    public int Id { get; set; }
    public string BossName { get; set; } = string.Empty;
    public int DifficultyRating { get; set; }
}

public sealed class BossGuideUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string BossName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string Tactics { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    [System.ComponentModel.DataAnnotations.Url]
    public string BossImageUrl { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, 10)]
    public int DifficultyRating { get; set; }

    public int RaidGuideId { get; set; }
}

public sealed class TalentBuildDto
{
    public int Id { get; set; }
    public string BuildName { get; set; } = string.Empty;
    public string TalentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public int PlayerProfileId { get; set; }
    public PlayerProfileSummaryDto? PlayerProfile { get; set; }
    public IReadOnlyList<TalentBuildFileDto> Files { get; set; } = Array.Empty<TalentBuildFileDto>();
}

public sealed class TalentBuildSummaryDto
{
    public int Id { get; set; }
    public string BuildName { get; set; } = string.Empty;
    public string TalentCode { get; set; } = string.Empty;
    public int PlayerProfileId { get; set; }
}

public sealed class TalentBuildUpsertDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string BuildName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(255)]
    public string TalentCode { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime? PublishedAt { get; set; }

    public int PlayerProfileId { get; set; }
}

public sealed class PlayerProfessionDto
{
    public int PlayerProfileId { get; set; }
    public int ProfessionId { get; set; }
    public int SkillLevel { get; set; }
    public PlayerProfileSummaryDto? PlayerProfile { get; set; }
    public ProfessionSummaryDto? Profession { get; set; }
}

public sealed class PlayerProfessionSummaryDto
{
    public int PlayerProfileId { get; set; }
    public int ProfessionId { get; set; }
    public int SkillLevel { get; set; }
    public string? PlayerName { get; set; }
    public string? ProfessionName { get; set; }
}

public sealed class PlayerProfessionUpsertDto
{
    public int PlayerProfileId { get; set; }
    public int ProfessionId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 450)]
    public int SkillLevel { get; set; }
}

public sealed class TalentBuildFileDto
{
    public int Id { get; set; }
    public int TalentBuildId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public sealed class BossGuideImageDto
{
    public int Id { get; set; }
    public int BossGuideId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public sealed class ApiPagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
}

public static class ApiDtoMapper
{
    public static PlayerProfileSummaryDto ToSummaryDto(this PlayerProfile player) => new()
    {
        Id = player.Id,
        CharacterName = player.CharacterName,
        Level = player.Level,
        ClassType = player.ClassType
    };

    public static GuildSummaryDto ToSummaryDto(this Guild guild) => new()
    {
        Id = guild.Id,
        Name = guild.Name,
        Realm = guild.Realm,
        MemberCount = guild.Members?.Count ?? 0
    };

    public static ProfessionSummaryDto ToSummaryDto(this Profession profession) => new()
    {
        Id = profession.Id,
        Name = profession.Name,
        BenefitDescription = profession.BenefitDescription
    };

    public static RaidGuideSummaryDto ToSummaryDto(this RaidGuide raid) => new()
    {
        Id = raid.Id,
        RaidName = raid.RaidName,
        BossCount = raid.Bosses?.Count ?? 0
    };

    public static BossGuideSummaryDto ToSummaryDto(this BossGuide boss) => new()
    {
        Id = boss.Id,
        BossName = boss.BossName,
        DifficultyRating = boss.DifficultyRating
    };

    public static TalentBuildSummaryDto ToSummaryDto(this TalentBuild build) => new()
    {
        Id = build.Id,
        BuildName = build.BuildName,
        TalentCode = build.TalentCode,
        PlayerProfileId = build.PlayerProfileId
    };

    public static PlayerProfessionSummaryDto ToSummaryDto(this PlayerProfession playerProfession) => new()
    {
        PlayerProfileId = playerProfession.PlayerProfileId,
        ProfessionId = playerProfession.ProfessionId,
        SkillLevel = playerProfession.SkillLevel,
        PlayerName = playerProfession.PlayerProfile?.CharacterName,
        ProfessionName = playerProfession.Profession?.Name
    };

    public static TalentBuildFileDto ToDto(this TalentBuildAttachment attachment) => new()
    {
        Id = attachment.Id,
        TalentBuildId = attachment.TalentBuildId,
        OriginalFileName = attachment.OriginalFileName,
        StoredFileName = attachment.StoredFileName,
        FileUrl = attachment.StoredFilePath.StartsWith('/') ? attachment.StoredFilePath : $"/{attachment.StoredFilePath}",
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        UploadedAt = attachment.UploadedAt
    };

    public static BossGuideImageDto ToDto(this BossGuideImage image) => new()
    {
        Id = image.Id,
        BossGuideId = image.BossGuideId,
        OriginalFileName = image.OriginalFileName,
        StoredFileName = image.StoredFileName,
        FileUrl = image.StoredFilePath.StartsWith('/') ? image.StoredFilePath : $"/{image.StoredFilePath}",
        ContentType = image.ContentType,
        FileSize = image.FileSize,
        UploadedAt = image.UploadedAt
    };

    public static PlayerProfileDto ToDto(this PlayerProfile player) => new()
    {
        Id = player.Id,
        CharacterName = player.CharacterName,
        Level = player.Level,
        ClassType = player.ClassType,
        LastUpdatedAt = player.LastUpdatedAt,
        GuildId = player.GuildId,
        Guild = player.Guild?.ToSummaryDto(),
        TalentBuilds = player.TalentBuilds is null
            ? Array.Empty<TalentBuildSummaryDto>()
            : player.TalentBuilds
                .OrderByDescending(build => build.PublishedAt)
                .Select(build => build.ToSummaryDto())
                .ToList(),
        Professions = player.Professions is null
            ? Array.Empty<PlayerProfessionDto>()
            : player.Professions
                .OrderBy(link => link.ProfessionId)
                .Select(link => new PlayerProfessionDto
                {
                    PlayerProfileId = link.PlayerProfileId,
                    ProfessionId = link.ProfessionId,
                    SkillLevel = link.SkillLevel,
                    Profession = link.Profession?.ToSummaryDto()
                })
                .ToList()
    };

    public static GuildDto ToDto(this Guild guild) => new()
    {
        Id = guild.Id,
        Name = guild.Name,
        Realm = guild.Realm,
        CreatedAt = guild.CreatedAt,
        Members = guild.Members is null
            ? Array.Empty<PlayerProfileSummaryDto>()
            : guild.Members
                .OrderBy(member => member.CharacterName)
                .Select(member => member.ToSummaryDto())
                .ToList()
    };

    public static ProfessionDto ToDto(this Profession profession) => new()
    {
        Id = profession.Id,
        Name = profession.Name,
        BenefitDescription = profession.BenefitDescription,
        Players = profession.Players is null
            ? Array.Empty<PlayerProfessionSummaryDto>()
            : profession.Players
                .OrderBy(link => link.PlayerProfile?.CharacterName)
                .Select(link => link.ToSummaryDto())
                .ToList()
    };

    public static RaidGuideDto ToDto(this RaidGuide raid) => new()
    {
        Id = raid.Id,
        RaidName = raid.RaidName,
        PreparationNotes = raid.PreparationNotes,
        UpdatedAt = raid.UpdatedAt,
        Bosses = raid.Bosses is null
            ? Array.Empty<BossGuideSummaryDto>()
            : raid.Bosses
                .OrderBy(boss => boss.DifficultyRating)
                .ThenBy(boss => boss.BossName)
                .Select(boss => boss.ToSummaryDto())
                .ToList()
    };

    public static BossGuideDto ToDto(this BossGuide boss) => new()
    {
        Id = boss.Id,
        BossName = boss.BossName,
        Tactics = boss.Tactics,
        BossImageUrl = boss.BossImageUrl,
        DifficultyRating = boss.DifficultyRating,
        RaidGuideId = boss.RaidGuideId,
        RaidGuide = boss.RaidGuide?.ToSummaryDto()
    };

    public static TalentBuildDto ToDto(this TalentBuild build) => new()
    {
        Id = build.Id,
        BuildName = build.BuildName,
        TalentCode = build.TalentCode,
        Description = build.Description,
        PublishedAt = build.PublishedAt,
        PlayerProfileId = build.PlayerProfileId,
        PlayerProfile = build.PlayerProfile?.ToSummaryDto(),
        Files = build.Attachments is null
            ? Array.Empty<TalentBuildFileDto>()
            : build.Attachments
                .OrderBy(file => file.UploadedAt)
                .Select(file => file.ToDto())
                .ToList()
    };

    public static PlayerProfessionDto ToDto(this PlayerProfession link) => new()
    {
        PlayerProfileId = link.PlayerProfileId,
        ProfessionId = link.ProfessionId,
        SkillLevel = link.SkillLevel,
        PlayerProfile = link.PlayerProfile?.ToSummaryDto(),
        Profession = link.Profession?.ToSummaryDto()
    };
}