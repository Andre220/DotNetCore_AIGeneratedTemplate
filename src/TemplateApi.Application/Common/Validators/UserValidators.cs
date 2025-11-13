using FluentValidation;
using TemplateApi.Application.Common.DTOs;

namespace TemplateApi.Application.Common.Validators;

/// <summary>
/// CONCEITO: Fluent Validation
/// 
/// FluentValidation é uma biblioteca para validação declarativa e fluente.
/// É superior aos DataAnnotations por vários motivos:
/// 
/// FLUENTVALIDATION vs DATA ANNOTATIONS:
/// 
/// Data Annotations (antigo):
/// ```csharp
/// public class CreateUserRequest
/// {
///     [Required]
///     [StringLength(100, MinimumLength = 3)]
///     public string Name { get; set; }
///     
///     [Required]
///     [EmailAddress]
///     public string Email { get; set; }
/// }
/// ```
/// ❌ Mistura validação com modelo
/// ❌ Difícil fazer validações complexas
/// ❌ Difícil testar
/// ❌ Menos flexível
/// 
/// FluentValidation (moderno):
/// ```csharp
/// public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
/// {
///     public CreateUserRequestValidator()
///     {
///         RuleFor(x => x.Name)
///             .NotEmpty()
///             .Length(3, 100);
///             
///         RuleFor(x => x.Email)
///             .NotEmpty()
///             .EmailAddress();
///     }
/// }
/// ```
/// ✅ Separação de responsabilidades
/// ✅ Validações complexas fáceis
/// ✅ Fácil de testar
/// ✅ Muito flexível
/// ✅ Async validators
/// ✅ Mensagens customizadas
/// ✅ Validações condicionais
/// 
/// INTEGRAÇÃO COM ASP.NET CORE:
/// 
/// 1. Instalar FluentValidation.AspNetCore
/// 2. Registrar no DI:
///    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
/// 3. Adicionar middleware/filter para validar automaticamente
/// 
/// EXEMPLO DE USO:
/// 
/// [HttpPost]
/// public async Task<IActionResult> Create([FromBody] CreateUserRequestDto request)
/// {
///     // Se chegou aqui, request já foi validado!
///     // Se tivesse erros, middleware já teria retornado 400 BadRequest
///     
///     var user = await _service.CreateUserAsync(request);
///     return Ok(user);
/// }
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        // VALIDAÇÃO DE NOME
        RuleFor(x => x.FullName)
            .NotEmpty()
                .WithMessage("Full name is required")
            .Length(3, 100)
                .WithMessage("Full name must be between 3 and 100 characters")
            .Matches(@"^[a-zA-Z\s]+$")
                .WithMessage("Full name can only contain letters and spaces");

        // VALIDAÇÃO DE EMAIL
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required")
            .EmailAddress()
                .WithMessage("Invalid email format")
            .MaximumLength(255)
                .WithMessage("Email cannot exceed 255 characters");

        // VALIDAÇÃO DE SENHA
        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("Password is required")
            .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
                .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]")
                .WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]")
                .WithMessage("Password must contain at least one special character");

        // VALIDAÇÃO DE CONFIRMAÇÃO DE SENHA
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
                .WithMessage("Password confirmation is required")
            .Equal(x => x.Password)
                .WithMessage("Passwords do not match");
    }
}

/// <summary>
/// Validator para atualização de usuário
/// 
/// NOTA: Senha não está aqui pois atualização de senha é endpoint separado
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
                .WithMessage("Full name is required")
            .Length(3, 100)
                .WithMessage("Full name must be between 3 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required")
            .EmailAddress()
                .WithMessage("Invalid email format");
    }
}

/// <summary>
/// Validator para login
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
                .WithMessage("Email is required")
            .EmailAddress()
                .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithMessage("Password is required");
    }
}

/// <summary>
/// Validator para paginação
/// 
/// IMPORTANTE: Sempre valide e limite inputs de paginação para evitar abuso
/// </summary>
public class PagedRequestValidator : AbstractValidator<PagedRequestDto>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
                .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
                .WithMessage("Page size cannot exceed 100");
    }
}

/// <summary>
/// EXEMPLO DE VALIDAÇÕES AVANÇADAS
/// 
/// FluentValidation permite validações muito complexas:
/// </summary>
public class AdvancedValidationExamples : AbstractValidator<CreateUserRequestDto>
{
    public AdvancedValidationExamples()
    {
        // VALIDAÇÃO CONDICIONAL
        // Só valida ConfirmPassword se Password foi fornecido
        When(x => !string.IsNullOrEmpty(x.Password), () =>
        {
            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password);
        });

        // VALIDAÇÃO ASSÍNCRONA
        // Exemplo: Verificar se email já existe no banco
        // RuleFor(x => x.Email)
        //     .MustAsync(async (email, cancellation) =>
        //     {
        //         return !await _userRepository.EmailExistsAsync(email);
        //     })
        //     .WithMessage("Email already exists");

        // VALIDAÇÃO CUSTOMIZADA
        RuleFor(x => x.Password)
            .Must(BeAValidPassword)
                .WithMessage("Password does not meet security requirements");

        // VALIDAÇÃO COM MÚLTIPLAS PROPRIEDADES
        RuleFor(x => x)
            .Must(x => x.Password == x.ConfirmPassword)
                .WithMessage("Passwords must match")
                .WithName("Password");
    }

    private bool BeAValidPassword(string password)
    {
        // Lógica customizada complexa aqui
        return password.Length >= 8 && 
               password.Any(char.IsUpper) && 
               password.Any(char.IsLower) && 
               password.Any(char.IsDigit);
    }
}
