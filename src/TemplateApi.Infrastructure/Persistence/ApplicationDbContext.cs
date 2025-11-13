using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Domain.Common;
using TemplateApi.Domain.Entities;

namespace TemplateApi.Infrastructure.Persistence;

/// <summary>
/// CONCEITO: DbContext (Entity Framework Core)
/// 
/// O DbContext é o ponto central do EF Core. Ele:
/// - Representa uma sessão com o banco de dados
/// - Permite fazer queries e salvar dados
/// - Rastreia mudanças nas entidades (Change Tracking)
/// - Gerencia transações
/// 
/// BOAS PRÁTICAS:
/// 1. Use DbContext como Scoped (uma instância por request HTTP)
/// 2. Sempre use async/await para operações de BD
/// 3. Configure entidades via Fluent API (não Data Annotations)
/// 4. Implemente Soft Delete e Audit automaticamente
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// DbSet representa uma tabela no banco de dados
    /// Cada DbSet<T> permite fazer queries LINQ sobre a entidade T
    /// </summary>
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// OnModelCreating: Configure o schema do banco usando Fluent API
    /// 
    /// POR QUE FLUENT API AO INVÉS DE DATA ANNOTATIONS?
    /// ✅ Fluent API:
    ///    - Separa configuração de BD da entidade de domínio
    ///    - Mais poderoso e flexível
    ///    - Mantém entidades de domínio limpas (sem dependência de EF)
    /// 
    /// ❌ Data Annotations:
    ///    - Polui a entidade de domínio com conceitos de infraestrutura
    ///    - Menos flexível
    ///    - Cria acoplamento entre domínio e EF
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas as configurações do assembly
        // As configurações estão em arquivos separados (UserConfiguration, etc)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // GLOBAL QUERY FILTER: Implementa Soft Delete automaticamente
        // Todas as queries vão automaticamente filtrar IsDeleted = false
        // Para incluir deletados, use: context.Users.IgnoreQueryFilters()
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateIsDeletedFilter(entityType.ClrType));
            }
        }
    }

    /// <summary>
    /// Gera lambda expression para filtrar IsDeleted = false
    /// Exemplo gerado: e => !e.IsDeleted
    /// </summary>
    private static LambdaExpression GenerateIsDeletedFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var notDeleted = System.Linq.Expressions.Expression.Not(property);
        return System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);
    }

    /// <summary>
    /// SaveChangesAsync: Sobrescrever para adicionar Audit Trail automático
    /// 
    /// AUDIT TRAIL:
    /// - Preenche CreatedAt quando adiciona uma entidade
    /// - Atualiza UpdatedAt quando modifica uma entidade
    /// - Usa UTC para evitar problemas de timezone
    /// 
    /// CHANGE TRACKER:
    /// O EF rastreia o estado de cada entidade:
    /// - Added: Nova entidade, será inserida
    /// - Modified: Entidade existente foi alterada
    /// - Deleted: Entidade será deletada (mas usamos soft delete)
    /// - Unchanged: Entidade não foi modificada
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Pegar todas as entidades que foram adicionadas ou modificadas
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Nova entidade: preenche CreatedAt usando Property do EF
                entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Entidade modificada: atualiza UpdatedAt usando Property do EF
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            }
        }

        // Salvar no banco de dados
        return await base.SaveChangesAsync(cancellationToken);
    }
}
