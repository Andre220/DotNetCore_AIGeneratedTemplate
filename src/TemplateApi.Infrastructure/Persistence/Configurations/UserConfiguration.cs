using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TemplateApi.Domain.Entities;

namespace TemplateApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// CONCEITO: Entity Configuration (Fluent API)
/// 
/// Aqui configuramos como a entidade User será mapeada para o banco de dados.
/// Fluent API é preferível a Data Annotations porque:
/// 
/// 1. SEPARAÇÃO DE CONCERNS: Domínio não depende de EF
/// 2. MAIS PODEROSO: Permite configurações que Data Annotations não suporta
/// 3. TESTABILIDADE: Entidades puras são mais fáceis de testar
/// 
/// CONFIGURAÇÕES IMPORTANTES:
/// - Chaves primárias e estrangeiras
/// - Índices (performance!)
/// - Constraints (unique, check)
/// - Nomes de tabelas e colunas
/// - Relacionamentos
/// - Value Conversions
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Nome da tabela
        builder.ToTable("Users");

        // Chave primária (geralmente já é configurada por convenção)
        builder.HasKey(u => u.Id);

        // PROPRIEDADES COM RESTRIÇÕES
        
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Nome completo do usuário");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Email único do usuário");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("Hash BCrypt da senha");

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indica se o email foi confirmado");

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indica se o usuário está ativo");

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false)
            .HasComment("Data/hora do último login");

        // ÍNDICES (PERFORMANCE!)
        
        // Índice único no email (garante que não há emails duplicados)
        // + melhora performance de queries por email
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // Índice composto: buscar usuários ativos não deletados
        builder.HasIndex(u => new { u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_Users_IsActive_IsDeleted");

        // Índice em CreatedAt para queries ordenadas por data
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // CONVERSÕES DE VALOR
        
        // Armazena Email sempre em lowercase
        builder.Property(u => u.Email)
            .HasConversion(
                v => v.ToLowerInvariant(), // Para o banco
                v => v.ToLowerInvariant()  // Do banco
            );

        // CONFIGURAÇÕES DE AUDITORIA (BaseEntity)
        
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasComment("Data/hora de criação (UTC)");

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false)
            .HasComment("Data/hora da última atualização (UTC)");

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Soft delete flag");

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100)
            .IsRequired(false);

        // RELACIONAMENTOS
        
        builder.HasMany<Order>()
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Não permite deletar user com orders
    }
}
