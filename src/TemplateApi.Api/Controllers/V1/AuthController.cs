using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateApi.Application.Features.Auth.Login;
using TemplateApi.Application.Features.Auth.Register;

namespace TemplateApi.Api.Controllers.V1;

/// <summary>
/// CONCEITO: Controllers + Minimal APIs
/// 
/// ASP.NET Core suporta duas abordagens:
/// 
/// 1. CONTROLLERS (tradicional MVC):
///    - Orientado a classes
///    - Familiar para quem vem do MVC
///    - Melhor para APIs grandes e complexas
///    - Suporta filtros, model binding, etc
/// 
/// 2. MINIMAL APIs (.NET 6+):
///    - Orientado a funções
///    - Menos código boilerplate
///    - Melhor performance
///    - Ideal para microserviços simples
/// 
/// Este projeto usa CONTROLLERS para demonstrar padrões enterprise.
/// 
/// ATRIBUTOS IMPORTANTES:
/// [ApiController] - Habilita comportamentos API-friendly
/// [Route] - Define rota do controller
/// [Authorize] - Exige autenticação
/// [AllowAnonymous] - Permite acesso sem autenticação
/// [HttpGet/Post/Put/Delete] - Verbos HTTP
/// [ProducesResponseType] - Documenta respostas possíveis
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        RegisterUserCommandHandler registerHandler,
        LoginCommandHandler loginHandler,
        ILogger<AuthController> logger)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário
    /// </summary>
    /// <param name="command">Dados do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário criado e token JWT</returns>
    /// <response code="200">Usuário registrado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user with email: {Email}", command.Email);

        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to register user: {Errors}", string.Join(", ", result.Errors));
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Falha ao registrar usuário",
                Detail = string.Join(", ", result.Errors)
            });
        }

        _logger.LogInformation("User registered successfully: {UserId}", result.Value!.UserId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Realiza login de usuário
    /// </summary>
    /// <param name="command">Email e senha</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token JWT</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Credenciais inválidas</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", command.Email);

        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", command.Email);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Falha ao fazer login",
                Detail = string.Join(", ", result.Errors)
            });
        }

        _logger.LogInformation("User logged in successfully: {UserId}", result.Value!.UserId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Verifica se o usuário está autenticado (teste)
    /// </summary>
    /// <returns>Informações do usuário autenticado</returns>
    [HttpGet("me")]
    [Authorize] // Exige JWT válido
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        // Claims do JWT estão em User.Claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = User.FindFirst("name")?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Name = name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    /// <summary>
    /// Logout (invalida token no lado do cliente)
    /// </summary>
    /// <remarks>
    /// JWT é stateless, então não há como "invalidar" no servidor.
    /// O cliente deve simplesmente deletar o token.
    /// 
    /// Para invalidação real, você precisaria:
    /// 1. Blacklist de tokens (Redis)
    /// 2. Refresh tokens
    /// 3. Short-lived tokens
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        _logger.LogInformation("User logged out: {UserId}", 
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        return Ok(new { message = "Logout realizado. Delete o token no cliente." });
    }
}
