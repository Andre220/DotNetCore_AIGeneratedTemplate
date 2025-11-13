using TemplateApi.Domain.Entities;

namespace TemplateApi.Domain.Interfaces;

/// <summary>
/// CONCEITO: Repository Específico
/// 
/// Além dos métodos genéricos do IRepository<T>, podemos adicionar métodos
/// específicos para cada entidade.
/// 
/// QUANDO CRIAR REPOSITÓRIOS ESPECÍFICOS:
/// 
/// 1. Queries complexas específicas da entidade
///    Exemplo: GetUsersByAgeRangeAndCity()
/// 
/// 2. Operações de negócio que não são CRUD simples
///    Exemplo: GetTopSellingProducts()
/// 
/// 3. Queries com joins e includes específicos
///    Exemplo: GetUserWithOrdersAndAddress()
/// 
/// 4. Queries otimizadas para casos de uso específicos
///    Exemplo: GetActiveUsersWithRecentOrders()
/// 
/// REGRA DE OURO:
/// Se a query é usada em apenas um lugar → coloque no handler/service
/// Se a query é reutilizada em vários lugares → coloque no repository
/// 
/// EXEMPLO DE USO:
/// 
/// // Handler/Service
/// public class GetUserByEmailHandler
/// {
///     private readonly IUserRepository _repository;
///     
///     public async Task<Result<UserDto>> Handle(GetUserByEmailQuery query)
///     {
///         // Usa método específico do IUserRepository
///         var user = await _repository.GetByEmailAsync(query.Email);
///         
///         if (user == null)
///             return Result<UserDto>.Failure("User not found");
///         
///         return Result<UserDto>.Success(MapToDto(user));
///     }
/// }
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Busca usuário por email
    /// 
    /// IMPORTANTE: Email deve ser único (adicionar índice único no BD)
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se email já existe
    /// 
    /// Mais eficiente que GetByEmailAsync quando só precisa saber se existe
    /// (não precisa trazer todos os dados do usuário)
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuários ativos (IsActive = true e IsDeleted = false)
    /// com paginação
    /// </summary>
    Task<(List<User> Users, int TotalCount)> GetActiveUsersPagedAsync(
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuários que não confirmaram email
    /// Útil para enviar lembretes de confirmação
    /// </summary>
    Task<List<User>> GetUsersWithUnconfirmedEmailAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuários inativos há mais de X dias
    /// Útil para campanhas de reengajamento
    /// </summary>
    Task<List<User>> GetInactiveUsersSinceAsync(
        DateTime since, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuários por parte do nome ou email
    /// Para funcionalidade de busca/autocomplete
    /// 
    /// IMPORTANTE: Use LIKE com índice ou Full Text Search em produção
    /// </summary>
    Task<List<User>> SearchAsync(
        string searchTerm, 
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}
