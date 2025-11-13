using TemplateApi.Domain.Common;

namespace TemplateApi.Domain.Interfaces;

/// <summary>
/// CONCEITO: Repository Pattern
/// 
/// O Repository é um padrão que abstrai o acesso a dados, fazendo com que
/// o domínio não precise saber COMO os dados são persistidos (SQL, NoSQL, arquivo, API, etc).
/// 
/// BENEFÍCIOS:
/// 1. Desacoplamento - Domain não depende de tecnologia específica (EF Core, Dapper, etc)
/// 2. Testabilidade - Fácil criar mocks para testes
/// 3. Centralização - Queries complexas ficam no repository, não espalhadas
/// 4. Troca de tecnologia - Posso trocar EF por Dapper sem afetar o domínio
/// 
/// IMPORTANTE:
/// - Repository é uma INTERFACE no Domain
/// - Implementation fica na Infrastructure
/// - Domain define O QUÊ precisa, Infrastructure define COMO
/// 
/// EXEMPLO DE USO:
/// 
/// // No Domain - apenas interface
/// public interface IUserRepository : IRepository<User>
/// {
///     Task<User> GetByEmailAsync(string email);
/// }
/// 
/// // Na Infrastructure - implementação com EF Core
/// public class UserRepository : Repository<User>, IUserRepository
/// {
///     public async Task<User> GetByEmailAsync(string email)
///     {
///         return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
///     }
/// }
/// 
/// // No Service/Handler - usa a interface
/// public class CreateUserHandler
/// {
///     private readonly IUserRepository _repository;
///     
///     public async Task<Result> Handle(CreateUserCommand command)
///     {
///         // Não sei se é EF, Dapper, MongoDB... e não preciso saber!
///         var existingUser = await _repository.GetByEmailAsync(command.Email);
///         // ...
///     }
/// }
/// </summary>
/// <typeparam name="T">Entidade que herda de BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Busca por ID
    /// Retorna null se não encontrar
    /// </summary>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todos (cuidado com tabelas grandes!)
    /// </summary>
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca com paginação
    /// 
    /// SEMPRE use paginação em produção para evitar:
    /// - OutOfMemoryException
    /// - Timeout de queries
    /// - Consumo excessivo de banda
    /// </summary>
    Task<(List<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova entidade
    /// 
    /// IMPORTANTE: Isso NÃO salva no banco ainda!
    /// Precisa chamar UnitOfWork.SaveChangesAsync() depois
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona múltiplas entidades de uma vez
    /// Mais eficiente que múltiplos AddAsync
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma entidade
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Remove do banco (hard delete)
    /// 
    /// CUIDADO: Em produção, prefira soft delete (entity.MarkAsDeleted())
    /// Hard delete pode:
    /// - Quebrar integridade referencial
    /// - Perder dados históricos
    /// - Impedir auditoria
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Remove múltiplas entidades
    /// </summary>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Conta quantos registros existem
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe algum registro que atende a condição
    /// 
    /// EXEMPLO:
    /// var emailExists = await _repository.ExistsAsync(u => u.Email == email);
    /// </summary>
    Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
