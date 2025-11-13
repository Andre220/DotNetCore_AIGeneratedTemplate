using Microsoft.EntityFrameworkCore;
using TemplateApi.Domain.Entities;
using TemplateApi.Domain.Interfaces;

namespace TemplateApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// CONCEITO: Specific Repository
/// 
/// UserRepository herda operações básicas do Repository<T> genérico,
/// mas adiciona queries específicas de domínio que só fazem sentido para User.
/// 
/// REGRA:
/// - Operações CRUD genéricas → Repository<T>
/// - Queries específicas de domínio → UserRepository
/// 
/// EXEMPLO:
/// - GetByIdAsync() → Genérico (Repository<T>)
/// - GetByEmailAsync() → Específico (UserRepository)
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Busca usuário por email
    /// QUERY ESPECÍFICA: Só User tem email como identificador único
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    /// <summary>
    /// Verifica se email já existe
    /// PERFORMANCE: Mais rápido que GetByEmailAsync() porque não carrega a entidade
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    /// <summary>
    /// Busca usuários ativos com paginação
    /// QUERY DE DOMÍNIO: Filtro específico de negócio
    /// </summary>
    public async Task<(List<User> Users, int TotalCount)> GetActiveUsersPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.IsActive && u.EmailConfirmed);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    /// <summary>
    /// Busca usuários com email não confirmado
    /// Útil para enviar lembretes de confirmação
    /// </summary>
    public async Task<List<User>> GetUsersWithUnconfirmedEmailAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => !u.EmailConfirmed)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca usuários inativos desde uma data específica
    /// Útil para campanhas de reengajamento
    /// </summary>
    public async Task<List<User>> GetInactiveUsersSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.LastLoginAt.HasValue && u.LastLoginAt.Value < since)
            .OrderBy(u => u.LastLoginAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca usuários por nome ou email (autocomplete/search)
    /// LIKE QUERY: Útil para autocomplete/search
    /// </summary>
    public async Task<List<User>> SearchAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm))
            .OrderBy(u => u.FullName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
