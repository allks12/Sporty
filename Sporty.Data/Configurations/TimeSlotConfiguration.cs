using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot> {
    public void Configure(EntityTypeBuilder<TimeSlot> builder) {
        builder.HasKey(t => t.TimeSlotId);

        builder.Property(t => t.StartAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(t => t.EndAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        builder.Property(t => t.IsReserved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(t => t.Venue)
            .WithMany(v => v.TimeSlots)
            .HasForeignKey(t => t.VenueId)
            .IsRequired();

        builder.HasIndex(t => new {t.VenueId, t.StartAt});

        builder.ToTable(table => table.HasCheckConstraint("CK_TimeSlots_EndAfterStart", "\"EndAt\" > \"StartAt\""));
    }
}