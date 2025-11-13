using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateApi.Domain.Entities;

namespace TemplateApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuração do Aggregate Root Order
/// Demonstra relacionamento 1:N com OrderItem
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>(); // Enum -> int

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.CompletedAt)
            .IsRequired(false);

        builder.Property(o => o.CancelledAt)
            .IsRequired(false);

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        // RELACIONAMENTO com User
        builder.HasOne(o => o.User)
            .WithMany() // User não tem navegação para Orders neste exemplo
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // RELACIONAMENTO com OrderItems
        // Order é o agregado root, possui seus OrderItems
        builder.HasMany(o => o.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade); // Deletar Order deleta seus items

        // Índices
        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("IX_Orders_UserId");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_Orders_CreatedAt");

        // Audit trail
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired(false);
        builder.Property(o => o.IsDeleted).IsRequired().HasDefaultValue(false);

        // Ignore DomainEvents (não persistir no banco)
        builder.Ignore(o => o.DomainEvents);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.OrderId)
            .IsRequired();

        builder.Property(oi => oi.ProductId)
            .IsRequired();

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200)
            .HasComment("Nome do produto no momento da compra");

        builder.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasComment("Preço unitário no momento da compra");

        builder.Property(oi => oi.Quantity)
            .IsRequired();

        // Subtotal é calculado, não precisa persistir
        builder.Ignore(oi => oi.Subtotal);

        // Índices
        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        // Audit trail
        builder.Property(oi => oi.CreatedAt).IsRequired();
        builder.Property(oi => oi.UpdatedAt).IsRequired(false);
        builder.Property(oi => oi.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}
