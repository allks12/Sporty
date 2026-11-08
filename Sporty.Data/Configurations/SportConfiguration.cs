using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport> {
    public void Configure(EntityTypeBuilder<Sport> builder) {
        builder.HasKey(s => s.SportId);

        builder.Property(s => s.SportName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.SportName)
            .IsUnique();

        builder.HasData(
            new Sport { SportId = 1, SportName = "Футбол" },
            new Sport { SportId = 2, SportName = "Баскетбол" },
            new Sport { SportId = 3, SportName = "Волейбол" },
            new Sport { SportId = 4, SportName = "Тенис" },
            new Sport { SportId = 5, SportName = "Бадминтон" },
            new Sport { SportId = 6, SportName = "Тенис на маса" },
            new Sport { SportId = 7, SportName = "Хандбал" },
            new Sport { SportId = 8, SportName = "Ръгби" },
            new Sport { SportId = 9, SportName = "Бягане" },
            new Sport { SportId = 10, SportName = "Плуване" }
        );
    }
}