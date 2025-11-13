using FluentValidation;
using TemplateApi.Application.Common.Interfaces;
using TemplateApi.Domain.Common;
using TemplateApi.Domain.Entities;
using TemplateApi.Domain.Interfaces;

namespace TemplateApi.Application.Features.Auth.Register;

/// <summary>
/// CONCEITO: CQRS - Command
/// 
/// CQRS = Command Query Responsibility Segregation
/// Separa operações de leitura (Queries) de operações de escrita (Commands)
/// 
/// COMMAND:
/// - Modifica estado (Create, Update, Delete)
/// - Retorna sucesso/falha (não retorna dados)
/// - Pode ter validação complexa
/// - Pode disparar Domain Events
/// 
/// EXEMPLO:
/// Command: RegisterUserCommand → cria usuário
/// Query: GetUserByIdQuery → busca usuário
/// 
/// BENEFÍCIOS:
/// ✅ Código mais organizado
/// ✅ Fácil adicionar features (nova query/command)
/// ✅ Pode otimizar leitura e escrita separadamente
/// ✅ Escala melhor (pode ter BDs diferentes)
/// </summary>
public record RegisterUserCommand
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}

/// <summary>
/// DTO de resposta
/// Nunca retorne a entidade de domínio diretamente!
/// </summary>
public record RegisterUserResponse
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// CONCEITO: FluentValidation
/// 
/// FluentValidation é uma biblioteca para validação de entrada.
/// Separa lógica de validação da lógica de negócio.
/// 
/// VANTAGENS sobre Data Annotations:
/// ✅ Mais poderoso e flexível
/// ✅ Validações complexas (queries ao BD)
/// ✅ Testável isoladamente
/// ✅ Reutilizável
/// ✅ Mensagens de erro customizáveis
/// 
/// QUANDO EXECUTAR:
/// - Antes do handler
/// - MediatR Pipeline Behavior pode fazer isso automaticamente
/// - Ou endpoint valida antes de chamar
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter pelo menos 3 caracteres")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"\d").WithMessage("Senha deve conter pelo menos um número")
            .Matches(@"[!@#$%^&*(),.?\"":{}|<>]").WithMessage("Senha deve conter pelo menos um caractere especial");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Senhas não conferem");
    }
}

/// <summary>
/// CONCEITO: Command Handler
/// 
/// Handler processa o command e executa a lógica de negócio.
/// Um handler = uma responsabilidade (Single Responsibility Principle)
/// 
/// FLUXO:
/// 1. Validação (validator)
/// 2. Lógica de negócio (handler)
/// 3. Persistência (repository + unit of work)
/// 4. Side effects (events, email, etc)
/// 
/// VERTICAL SLICE:
/// Tudo relacionado a "Register User" está neste arquivo:
/// - Command (input)
/// - Response (output)
/// - Validator (validation)
/// - Handler (logic)
/// 
/// Fácil encontrar, modificar, testar!
/// </summary>
public class RegisterUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RegisterUserCommandValidator _validator;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IEmailService emailService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _validator = new RegisterUserCommandValidator();
    }

    public async Task<Result<RegisterUserResponse>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. VALIDAÇÃO
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result<RegisterUserResponse>.Failure(errors);
        }

        // 2. VERIFICAR SE EMAIL JÁ EXISTE
        var emailExists = await _userRepository.EmailExistsAsync(command.Email, cancellationToken);
        if (emailExists)
        {
            return Result<RegisterUserResponse>.Failure(new List<string> { "Email já está em uso" });
        }

        // 3. CRIAR ENTIDADE DE DOMÍNIO
        var passwordHash = _passwordHasher.HashPassword(command.Password);
        
        var userResult = User.Create(
            command.FullName,
            command.Email,
            passwordHash);

        if (userResult.IsFailure)
        {
            return Result<RegisterUserResponse>.Failure(userResult.Errors);
        }

        var user = userResult.Value!;

        // 4. PERSISTIR
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. GERAR TOKEN JWT
        var token = _jwtService.GenerateToken(user.Id, user.Email);

        // 6. ENVIAR EMAIL DE CONFIRMAÇÃO (Background job seria melhor)
        // TODO: Mover para background job
        try
        {
            var confirmationLink = $"https://api.example.com/auth/confirm-email?token={token}";
            await _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink, cancellationToken);
        }
        catch
        {
            // Não falha o registro se email não enviar
            // Log error aqui
        }

        // 7. RETORNAR RESPOSTA
        var response = new RegisterUserResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Token = token,
            Message = "Usuário registrado com sucesso! Verifique seu email."
        };

        return Result<RegisterUserResponse>.Success(response);
    }
}
