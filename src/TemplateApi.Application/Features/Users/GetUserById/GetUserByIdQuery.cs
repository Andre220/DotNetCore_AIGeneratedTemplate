using TemplateApi.Domain.Common;
using TemplateApi.Domain.Interfaces;

namespace TemplateApi.Application.Features.Users.GetUserById;

/// <summary>
/// CONCEITO: CQRS - Query
/// 
/// Query = operação de LEITURA (não modifica estado)
/// 
/// CARACTERÍSTICAS:
/// - Retorna dados
/// - Não tem efeitos colaterais
/// - Pode ser cacheada
/// - Pode usar DTOs otimizados (não precisa carregar entidade completa)
/// - Pode fazer queries diretas ao BD (sem passar pelo repositório)
/// </summary>
public record GetUserByIdQuery
{
    public int UserId { get; init; }
}

public record UserResponse
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetUserByIdQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserResponse>> HandleAsync(
        GetUserByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user == null)
        {
            return Result<UserResponse>.Failure(new List<string> { "Usuário não encontrado" });
        }

        var response = new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };

        return Result<UserResponse>.Success(response);
    }
}
