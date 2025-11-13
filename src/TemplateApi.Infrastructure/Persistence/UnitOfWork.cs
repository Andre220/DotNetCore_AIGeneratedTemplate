using TemplateApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace TemplateApi.Infrastructure.Persistence;

/// <summary>
/// CONCEITO: Unit of Work Pattern
/// 
/// O UnitOfWork coordena o trabalho de múltiplos repositórios e garante que
/// todas as mudanças sejam salvas em uma única transação.
/// 
/// PROBLEMA SEM UNIT OF WORK:
/// - await userRepository.AddAsync(user);
/// - await orderRepository.AddAsync(order);
/// 
/// Se o segundo falhar, o primeiro já foi salvo! Inconsistência!
/// 
/// SOLUÇÃO COM UNIT OF WORK:
/// - await userRepository.AddAsync(user);      // Não salva ainda
/// - await orderRepository.AddAsync(order);    // Não salva ainda
/// - await unitOfWork.CommitAsync();           // Salva tudo ou nada (transação)
/// 
/// BENEFÍCIOS:
/// 1. ATOMICIDADE: Ou salva tudo ou nada (princípio ACID)
/// 2. CONSISTÊNCIA: Dados sempre em estado válido
/// 3. PERFORMANCE: Uma única ida ao banco ao invés de múltiplas
/// 4. CONTROLE: Você decide quando persistir
/// 
/// ANALOGIA:
/// Imagine um carrinho de compras:
/// - Você adiciona vários items (repositories)
/// - Só quando clica "Finalizar Compra" que a venda é efetivada (commit)
/// - Se algo der errado, todos os items voltam para a prateleira (rollback)
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Aqui você pode adicionar logging
            // Log.Error("Erro ao salvar mudanças", ex);
            throw; // Re-lança a exception para o chamador tratar
        }
    }

    /// <summary>
    /// Persiste todas as mudanças no banco de dados em uma única transação
    /// 
    /// IMPORTANTE:
    /// - Se qualquer operação falhar, TODAS são revertidas (rollback automático)
    /// - Retorna o número de registros afetados
    /// 
    /// EXEMPLO DE USO:
    /// await _userRepository.AddAsync(user);
    /// await _orderRepository.AddAsync(order);
    /// var affectedRows = await _unitOfWork.CommitAsync(); // Salva ambos
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Aqui você pode adicionar logging
            // Log.Error("Erro ao salvar mudanças", ex);
            throw; // Re-lança a exception para o chamador tratar
        }
    }

    /// <summary>
    /// Descarta mudanças pendentes que ainda não foram commitadas
    /// 
    /// USO: Quando algo dá errado e você quer descartar todas as mudanças
    /// 
    /// ATENÇÃO: EF Core já faz rollback automático se CommitAsync falhar,
    /// então raramente você precisa chamar isso manualmente.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        // EF Core Change Tracker: reseta o estado de todas as entidades
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    break;
                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                    entry.Reload();
                    break;
            }
        }
    }

    /// <summary>
    /// Inicia uma transação explícita
    /// 
    /// USO AVANÇADO: Quando você precisa de controle granular sobre transações
    /// Normalmente CommitAsync já é suficiente.
    /// 
    /// EXEMPLO:
    /// using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     await _userRepository.AddAsync(user);
    ///     await _unitOfWork.CommitAsync();
    ///     
    ///     // Chama API externa
    ///     await _externalApi.NotifyAsync();
    ///     
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Libera recursos não gerenciados
    /// Pattern IDisposable
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }
}
