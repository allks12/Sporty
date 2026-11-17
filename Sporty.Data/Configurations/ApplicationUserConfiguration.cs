using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Identity;

namespace Sporty.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser> {
    public const int NameMaxLength = 100;

    public void Configure(EntityTypeBuilder<ApplicationUser> builder) {
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(NameMaxLength);

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");
    }
}
