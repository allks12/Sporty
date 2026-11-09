using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group> {
    public void Configure(EntityTypeBuilder<Group> builder) {
        builder.HasKey(g => g.GroupId);

        builder.Property(g => g.StartAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(g => g.MinMembers)
            .IsRequired(false);

        builder.Property(g => g.MaxMembers)
            .IsRequired();

        builder.Property(g => g.Status)
            .IsRequired()
            // Stores the enum as a string ("open", "confirmed" etc)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(GroupStatus.Open);

        builder.Property(g => g.CustomLocation)
            .HasMaxLength(300);

        builder.HasOne(g => g.Venue)
            .WithMany()
            .HasForeignKey(g => g.VenueId)
            .IsRequired(false);

        builder.HasOne(g => g.TimeSlot)
            .WithOne(t => t.Group)
            // Group entity contains the foreign key
            .HasForeignKey<Group>(g =>g.TimeSlotId)
            .IsRequired(false);

        builder.HasOne(g => g.District)
            .WithMany()
            .HasForeignKey(g => g.DistrictId)
            .IsRequired(false);

        builder.HasOne(g => g.Sport)
            .WithMany(s => s.Groups)
            .HasForeignKey(g => g.SportId)
            .IsRequired();

        builder.HasIndex(g => g.TimeSlotId)
            .IsUnique()
            .HasFilter("\"TimeSlotId\" IS NOT NULL");

        builder.HasIndex(g => g.SportId);
        builder.HasIndex(g => g.StartAt);
        builder.HasIndex(g => g.DistrictId);
        builder.HasIndex(g => g.VenueId);
        builder.HasIndex(g => g.Status);
    }
}