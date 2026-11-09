using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sporty.Data.Entities;

namespace Sporty.Data.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District> {
    public void Configure(EntityTypeBuilder<District> builder) {
        builder.HasKey(d => d.DistrictId);

        builder.Property(d => d.DistrictName)
            .IsRequired()
            .HasMaxLength(100);

            builder.HasIndex(d => d.DistrictName)
                .IsUnique();

        builder.HasData(
            new District { DistrictId = 1, DistrictName = "Средец" },
            new District { DistrictId = 2, DistrictName = "Красно село" },
            new District { DistrictId = 3, DistrictName = "Възраждане" },
            new District { DistrictId = 4, DistrictName = "Оборище" },
            new District { DistrictId = 5, DistrictName = "Сердика" },
            new District { DistrictId = 6, DistrictName = "Подуяне" },
            new District { DistrictId = 7, DistrictName = "Слатина" },
            new District { DistrictId = 8, DistrictName = "Изгрев" },
            new District { DistrictId = 9, DistrictName = "Лозенец" },
            new District { DistrictId = 10, DistrictName = "Триадица" },
            new District { DistrictId = 11, DistrictName = "Красна поляна" },
            new District { DistrictId = 12, DistrictName = "Илинден" },
            new District { DistrictId = 13, DistrictName = "Надежда" },
            new District { DistrictId = 14, DistrictName = "Искър" },
            new District { DistrictId = 15, DistrictName = "Младост" },
            new District { DistrictId = 16, DistrictName = "Студентски" },
            new District { DistrictId = 17, DistrictName = "Витоша" },
            new District { DistrictId = 18, DistrictName = "Овча купел" },
            new District { DistrictId = 19, DistrictName = "Люлин" },
            new District { DistrictId = 20, DistrictName = "Връбница" },
            new District { DistrictId = 21, DistrictName = "Нови Искър" },
            new District { DistrictId = 22, DistrictName = "Кремиковци" },
            new District { DistrictId = 23, DistrictName = "Панчарево" },
            new District { DistrictId = 24, DistrictName = "Банкя" }
        );
    }
}