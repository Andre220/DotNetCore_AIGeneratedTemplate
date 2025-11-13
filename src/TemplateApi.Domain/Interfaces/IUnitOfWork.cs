namespace TemplateApi.Domain.Interfaces;

/// <summary>
/// CONCEITO: Unit of Work Pattern
/// 
/// O Unit of Work coordena múltiplas operações de repositórios em uma ÚNICA transação.
/// Garante que todas as mudanças sejam salvas juntas ou nenhuma seja salva.
/// 
/// PROBLEMA SEM UNIT OF WORK:
/// 
/// // ❌ Cada SaveChanges é uma transação separada
/// await _userRepository.AddAsync(user);
/// await _userRepository.SaveAsync(); // Transação 1
/// 
/// await _profileRepository.AddAsync(profile);
/// await _profileRepository.SaveAsync(); // Transação 2
/// 
/// // Se a segunda falhar, a primeira já foi salva! Estado inconsistente!
/// 
/// 
/// SOLUÇÃO COM UNIT OF WORK:
/// 
/// // ✅ Tudo em uma transação
/// await _userRepository.AddAsync(user);
/// await _profileRepository.AddAsync(profile);
/// await _unitOfWork.SaveChangesAsync(); // Uma única transação!
/// // Se qualquer coisa falhar, nada é salvo (rollback automático)
/// 
/// BENEFÍCIOS:
/// 1. Atomicidade - Tudo ou nada (princípio ACID)
/// 2. Consistência - Estado sempre válido
/// 3. Performance - Uma única ida ao banco
/// 4. Simplicidade - Um único ponto para salvar
/// 
/// QUANDO USAR:
/// - Operações que modificam múltiplas entidades
/// - Operações críticas que não podem ficar pela metade
/// - Qualquer operação que exige consistência
/// 
/// EXEMPLO COMPLETO:
/// 
/// public class TransferMoneyHandler
/// {
///     private readonly IAccountRepository _accountRepo;
///     private readonly ITransactionRepository _transactionRepo;
///     private readonly IUnitOfWork _unitOfWork;
///     
///     public async Task<Result> Handle(TransferMoneyCommand cmd)
///     {
///         var from Account = await _accountRepo.GetByIdAsync(cmd.FromId);
///         var toAccount = await _accountRepo.GetByIdAsync(cmd.ToId);
///         
///         // Debita da conta origem
///         fromAccount.Debit(cmd.Amount);
///         _accountRepo.Update(fromAccount);
///         
///         // Credita na conta destino
///         toAccount.Credit(cmd.Amount);
///         _accountRepo.Update(toAccount);
///         
///         // Registra a transação
///         var transaction = Transaction.Create(cmd.FromId, cmd.ToId, cmd.Amount);
///         await _transactionRepo.AddAsync(transaction);
///         
///         // TUDO salvo de uma vez, em uma única transação!
///         await _unitOfWork.SaveChangesAsync();
///         
///         // Se qualquer coisa der errado, NADA é salvo
///         return Result.Success();
///     }
/// }
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Salva todas as mudanças pendentes em uma única transação
    /// 
    /// IMPORTANTE:
    /// - Dispara domain events automaticamente
    /// - Atualiza propriedades de auditoria (UpdatedAt, UpdatedBy)
    /// - Aplica soft delete filters
    /// - Faz rollback automático em caso de erro
    /// 
    /// RETORNO:
    /// Número de entidades afetadas
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia uma transação explícita
    /// 
    /// Use quando precisar de controle manual da transação:
    /// 
    /// using var transaction = await _unitOfWork.BeginTransactionAsync();
    /// try
    /// {
    ///     // operações...
    ///     await _unitOfWork.SaveChangesAsync();
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma (commit) a transação atual
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela (rollback) a transação atual
    /// Volta tudo ao estado antes de BeginTransaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
