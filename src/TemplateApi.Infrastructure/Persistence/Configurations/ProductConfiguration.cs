using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateApi.Domain.Entities;

namespace TemplateApi.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2) // 18 dígitos, 2 decimais
            .HasComment("Preço do produto em reais");

        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasComment("Quantidade em estoque");

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasConversion<int>() // Armazena enum como int
            .HasComment("Categoria do produto");

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        // Índices
        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("IX_Products_Sku");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Products_Category");

        builder.HasIndex(p => new { p.IsAvailable, p.IsDeleted })
            .HasDatabaseName("IX_Products_IsAvailable_IsDeleted");

        // Audit trail
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired(false);
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}
