using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TemplateApi.Application.Features.Users.GetAllUsers;
using TemplateApi.Application.Features.Users.GetUserById;

namespace TemplateApi.Api.Controllers.V1;

/// <summary>
/// Gerenciamento de usuários
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Todos os endpoints exigem autenticação
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly GetUserByIdQueryHandler _getUserByIdHandler;
    private readonly GetAllUsersQueryHandler _getAllUsersHandler;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        GetUserByIdQueryHandler getUserByIdHandler,
        GetAllUsersQueryHandler getAllUsersHandler,
        ILogger<UsersController> logger)
    {
        _getUserByIdHandler = getUserByIdHandler;
        _getAllUsersHandler = getAllUsersHandler;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os usuários com paginação
    /// </summary>
    /// <param name="pageNumber">Número da página (default: 1)</param>
    /// <param name="pageSize">Tamanho da página (default: 10)</param>
    /// <param name="searchTerm">Termo de busca (nome ou email)</param>
    /// <param name="isActive">Filtrar por status ativo</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de usuários</returns>
    /// <response code="200">Lista de usuários retornada com sucesso</response>
    [HttpGet]
    [ProducesResponseType(typeof(UsersListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive
        };

        var result = await _getAllUsersHandler.HandleAsync(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Erro ao buscar usuários",
            Detail = string.Join(", ", result.Errors)
        });
    }

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="404">Usuário não encontrado</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery { UserId = id };

        var result = await _getUserByIdHandler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Usuário não encontrado",
                Detail = string.Join(", ", result.Errors)
            });
        }

        return Ok(result.Value);
    }

    // TODO: Implementar Update, Delete, etc
}
