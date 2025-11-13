using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Domain.Common;
using TemplateApi.Domain.Interfaces;

namespace TemplateApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// CONCEITO: Generic Repository Pattern
/// 
/// Um repositório genérico que implementa operações CRUD comuns para qualquer entidade.
/// Evita duplicação de código nas operações básicas.
/// 
/// BENEFÍCIOS:
/// - DRY: Não repetir código CRUD em cada repositório
/// - Consistência: Todas as entidades usam o mesmo padrão
/// - Testabilidade: Fácil mockar para testes
/// 
/// QUANDO USAR:
/// - Operações CRUD simples
/// - Queries comuns (GetById, GetAll, etc)
/// 
/// QUANDO NÃO USAR:
/// - Queries complexas específicas de domínio
/// - Para esses casos, crie repositórios específicos (ex: IUserRepository)
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// Busca entidade por ID
    /// IMPORTANTE: Já filtra IsDeleted = false automaticamente (Query Filter)
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Lista todas as entidades
    /// PERFORMANCE: Use com cuidado! Considere paginação.
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca com filtro customizado
    /// Exemplo: await repository.FindAsync(u => u.Email == "teste@email.com");
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Busca um único item com filtro
    /// Retorna null se não encontrar
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Verifica se existe alguma entidade que atenda ao filtro
    /// PERFORMANCE: Mais rápido que Count() > 0
    /// </summary>
    public virtual async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Conta quantas entidades existem
    /// </summary>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Verifica se existe alguma entidade que atenda à condição
    /// PERFORMANCE: Mais rápido que Count() > 0
    /// </summary>
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Adiciona nova entidade
    /// ATENÇÃO: Não salva no banco ainda! Chame UnitOfWork.CommitAsync()
    /// </summary>
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adiciona múltiplas entidades de uma vez
    /// PERFORMANCE: Mais rápido que chamar AddAsync várias vezes
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Atualiza entidade
    /// ATENÇÃO: Não salva no banco ainda! Chame UnitOfWork.CommitAsync()
    /// </summary>
    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    /// <summary>
    /// Remove entidade (Hard Delete)
    /// ATENÇÃO: Deleta fisicamente do banco!
    /// Para soft delete, use entity.MarkAsDeleted() + Update()
    /// </summary>
    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Remove múltiplas entidades (Hard Delete)
    /// </summary>
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Remove entidade (Soft Delete)
    /// Marca IsDeleted = true ao invés de deletar fisicamente
    /// RECOMENDADO: Use este ao invés de Delete()
    /// </summary>
    public virtual void Remove(T entity)
    {
        entity.MarkAsDeleted();
        _dbSet.Update(entity);
    }

    /// <summary>
    /// Remove múltiplas entidades (Soft Delete)
    /// </summary>
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            entity.MarkAsDeleted();
        }
        _dbSet.UpdateRange(entities);
    }

    /// <summary>
    /// Paginação: busca página de resultados
    /// 
    /// EXEMPLO:
    /// var page = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);
    /// </summary>
    public virtual async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);

        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Paginação com filtro: busca página de resultados com predicado
    /// 
    /// EXEMPLO:
    /// var page = await repository.GetPagedAsync(1, 10, u => u.IsActive);
    /// </summary>
    public virtual async Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
