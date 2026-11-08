using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember> {
    public void Configure(EntityTypeBuilder<GroupMember> builder) {
        builder.HasKey(gm => gm.GroupMemberId);

        builder.Property(gm => gm.UserId)
            .IsRequired();

        builder.Property(gm => gm.JoinedAt)
            .IsRequired();

        builder.Property(gm => gm.ParticipantStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(ParticipantStatus.Registered);

        builder.HasOne(gm => gm.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(gm => gm.GroupId)
            .IsRequired();

        builder.HasOne(gm => gm.User)
            .WithMany()
            .HasForeignKey(gm => gm.UserId)
            .IsRequired();

        //Prevents one user from registering the same group twice
        builder.HasIndex(gm => new {gm.GroupId, gm.UserId})
            .IsUnique();
    }
}