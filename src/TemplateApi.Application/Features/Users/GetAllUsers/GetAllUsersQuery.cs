using TemplateApi.Domain.Common;
using TemplateApi.Domain.Interfaces;

namespace TemplateApi.Application.Features.Users.GetAllUsers;

public record GetAllUsersQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
}

public record UsersListResponse
{
    public List<UserListItem> Users { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record UserListItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailConfirmed { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetAllUsersQueryHandler
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UsersListResponse>> HandleAsync(
        GetAllUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        // Por simplicidade, busca todos os usuários
        // TODO: Implementar filtragem e paginação adequada
        var allUsers = await _userRepository.GetAllAsync(cancellationToken);

        // Aplicar filtros em memória (não ideal para produção, mas funciona para demonstração)
        var filteredUsers = allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            filteredUsers = filteredUsers.Where(u => 
                u.FullName.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) || 
                u.Email.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsActive.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive == query.IsActive.Value);
        }

        var totalCount = filteredUsers.Count();

        var users = filteredUsers
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var userItems = users.Select(u => new UserListItem
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            EmailConfirmed = u.EmailConfirmed,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();

        var response = new UsersListResponse
        {
            Users = userItems,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };

        return Result<UsersListResponse>.Success(response);
    }
}

/// <summary>
/// Helper para combinar expressions (AND)
/// </summary>
public static class ExpressionExtensions
{
    public static System.Linq.Expressions.Expression<Func<T, bool>> And<T>(
        this System.Linq.Expressions.Expression<Func<T, bool>> left,
        System.Linq.Expressions.Expression<Func<T, bool>> right)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var leftBody = leftVisitor.Visit(left.Body);

        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var rightBody = rightVisitor.Visit(right.Body);

        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            System.Linq.Expressions.Expression.AndAlso(leftBody!, rightBody!), parameter);
    }

    private class ReplaceExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly System.Linq.Expressions.Expression _oldValue;
        private readonly System.Linq.Expressions.Expression _newValue;

        public ReplaceExpressionVisitor(System.Linq.Expressions.Expression oldValue, System.Linq.Expressions.Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override System.Linq.Expressions.Expression? Visit(System.Linq.Expressions.Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}
