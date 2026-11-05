using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class VenueConfiguration : IEntityTypeConfiguration<Venue> {
    public void Configure(EntityTypeBuilder<Venue> builder) {
        builder.HasKey(v => v.VenueId);

        builder.Property(v => v.Address)
            .IsRequired();
        
        builder.Property(v => v.HourlyPrice)
            .HasPrecision(10,2)
            .IsRequired();

        builder.HasOne(v => v.Sport)
            .WithMany(s => s.Venues)
            .HasForeignKey(v => v.SportId)
            .IsRequired();

        builder.HasOne(v => v.District)
            .WithMany(d => d.Venues)
            .HasForeignKey(v => v.DistrictId)
            .IsRequired();

        builder.HasIndex(v => v.SportId);
        builder.HasIndex(v => v.DistrictId);
        builder.HasIndex(v => v.IsOutdoors);
    }
}