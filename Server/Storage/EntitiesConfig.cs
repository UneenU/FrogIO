using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Server.Data;

namespace Server.Storage;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
            table.HasCheckConstraint(
                "CK_Users_Rating_NonNegative",
                $"\"{nameof(User.Rating)}\" >= 0"));

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Nickname)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.Nickname);

        builder.Property(x => x.Rating)
            .HasDefaultValue(1000)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        
        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();
    }
}


public sealed class GameMatchConfiguration : IEntityTypeConfiguration<GameMatch>
{
    public void Configure(EntityTypeBuilder<GameMatch> builder)
    {
        builder.ToTable("Matches", table =>
            table.HasCheckConstraint(
                "CK_Matches_TimeOrder",
                $"\"{nameof(GameMatch.FinishedAtUtc)}\" >= \"{nameof(GameMatch.StartedAtUtc)}\""));

        builder.HasKey(x => x.MatchId);

        builder.Property(x => x.MatchId)
            .HasMaxLength(64)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.FinishedAtUtc)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.GameMode)
            .HasMaxLength(32);

        builder.HasIndex(x => new { x.Status, x.StartedAtUtc });
    }
}


public sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("MatchParticipants", table =>
        {
            table.HasCheckConstraint(
                "CK_MatchParticipants_Place_Positive",
                $"\"{nameof(MatchParticipant.Place)}\" >= 1");

            table.HasCheckConstraint(
                "CK_MatchParticipants_Score_NonNegative",
                $"\"{nameof(MatchParticipant.Score)}\" >= 0");
        });
        
        builder.HasKey(x => new { x.MatchId, x.UserId });

        builder.Property(x => x.MatchId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Place)
            .IsRequired();

        builder.Property(x => x.Score)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.RatingBefore)
            .IsRequired();

        builder.Property(x => x.RatingAfter)
            .IsRequired();

        builder.HasOne(x => x.Match)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany(x => x.MatchParticipants)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.MatchId });

        builder.HasIndex(x => new { x.MatchId, x.Place });
    }
}