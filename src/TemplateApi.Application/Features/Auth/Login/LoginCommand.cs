using FluentValidation;
using TemplateApi.Application.Common.Interfaces;
using TemplateApi.Domain.Common;
using TemplateApi.Domain.Interfaces;
using TemplateApi;

namespace TemplateApi.Application.Features.Auth.Login;

/// <summary>
/// VERTICAL SLICE: Login de usuário
/// Todo código relacionado a login está aqui
/// </summary>
public record LoginCommand
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; } = false;
}

public record LoginResponse
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória");
    }
}

public class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly LoginCommandValidator _validator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _validator = new LoginCommandValidator();
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. VALIDAÇÃO
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<LoginResponse>.Failure(errors);
        }

        // 2. BUSCAR USUÁRIO POR EMAIL
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
        {
            // SEGURANÇA: Mensagem genérica para não revelar se email existe
            return Result<LoginResponse>.Failure(new List<string> { "Email ou senha inválidos" });
        }

        // 3. VERIFICAR SENHA
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            // TODO: Implementar tentativas de login falhadas
            // Após X tentativas, bloquear conta temporariamente
            return Result<LoginResponse>.Failure(new List<string> { "Email ou senha inválidos" });
        }

        // 4. VERIFICAR SE USUÁRIO ESTÁ ATIVO
        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(new List<string> { "Conta desativada. Entre em contato com o suporte." });
        }

        // 5. VERIFICAR SE EMAIL FOI CONFIRMADO
        if (!user.EmailConfirmed)
        {
            return Result<LoginResponse>.Failure(new List<string> { "Email não confirmado. Verifique sua caixa de entrada." });
        }

        // 6. ATUALIZAR ÚLTIMO LOGIN
        // Note: UpdateLastLogin will be added to User entity
        // For now, just update via repository
        _userRepository.Update(user);

        // 7. GERAR TOKEN JWT
        // Claims adicionais podem ser adicionados aqui
        var additionalClaims = new Dictionary<string, string>
        {
            { "name", user.FullName }
        };

        var token = _jwtService.GenerateToken(user.Id, user.Email, additionalClaims);

        // 8. CALCULAR EXPIRAÇÃO
        // TODO: Pegar de configuração
        var expiresAt = DateTime.UtcNow.AddHours(command.RememberMe ? 720 : 24); // 30 dias ou 24h

        // 9. RETORNAR RESPOSTA
        var response = new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<LoginResponse>.Success(response);
    }
}
