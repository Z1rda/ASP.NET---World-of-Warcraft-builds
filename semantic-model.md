# Semantic model

## Entities and main properties
- PlayerProfile: Id, CharacterName, Level, ClassType, LastUpdatedAt, GuildId
- Guild: Id, Name, Realm, CreatedAt
- Profession: Id, Name, BenefitDescription
- PlayerProfession: PlayerProfileId, ProfessionId, SkillLevel
- TalentBuild: Id, BuildName, TalentCode, Description, PublishedAt, PlayerProfileId
- RaidGuide: Id, RaidName, PreparationNotes, UpdatedAt
- BossGuide: Id, BossName, Tactics, DifficultyRating, BossImageUrl, RaidGuideId

## Relationships
- Guild (1) -> (many) PlayerProfile via PlayerProfile.GuildId (nullable)
- PlayerProfile (1) -> (many) TalentBuild via TalentBuild.PlayerProfileId
- PlayerProfile (many) <-> (many) Profession through PlayerProfession
- RaidGuide (1) -> (many) BossGuide via BossGuide.RaidGuideId

## Notes
- ClassType is stored as an int enum on PlayerProfile.
- PlayerProfession uses a composite key (PlayerProfileId, ProfessionId).
