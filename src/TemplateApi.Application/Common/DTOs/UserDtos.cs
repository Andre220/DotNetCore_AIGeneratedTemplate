namespace TemplateApi.Application.Common.DTOs;

/// <summary>
/// CONCEITO: DTO (Data Transfer Object)
/// 
/// DTOs são objetos simples usados para transferir dados entre camadas.
/// NUNCA exponha entidades de domínio diretamente!
/// 
/// POR QUÊ USAR DTOs:
/// 
/// 1. DESACOPLAMENTO
///    - API não depende do modelo de domínio
///    - Posso mudar domínio sem quebrar contratos da API
///    
/// 2. SEGURANÇA
///    - Não exponho propriedades sensíveis (PasswordHash, por exemplo)
///    - Controlo exatamente o que vai/vem da API
///    
/// 3. PERFORMANCE
///    - Posso criar DTOs otimizados (apenas campos necessários)
///    - Evito lazy loading acidental
///    
/// 4. VERSIONAMENTO
///    - Posso ter UserDtoV1 e UserDtoV2 apontando para mesma entidade
///    
/// 5. CONTRATOS EXPLÍCITOS
///    - DTO deixa claro o que a API aceita/retorna
///    - Facilita documentação (Swagger)
/// 
/// EXEMPLO PROBLEMA SEM DTO:
/// 
/// // ❌ Expondo entidade diretamente
/// [HttpGet]
/// public async Task<User> Get(int id)
/// {
///     return await _repository.GetByIdAsync(id);
///     // Problema: expõe PasswordHash, emails de auditoria, etc!
/// }
/// 
/// // ✅ Usando DTO
/// [HttpGet]
/// public async Task<UserResponseDto> Get(int id)
/// {
///     var user = await _repository.GetByIdAsync(id);
///     return UserResponseDto.FromEntity(user);
///     // Expõe apenas o que deve ser público
/// }
/// 
/// TIPOS DE DTOs:
/// - Request DTOs: dados que chegam na API
/// - Response DTOs: dados que saem da API
/// - Command DTOs: operações que modificam estado (CQRS)
/// - Query DTOs: operações que apenas leem (CQRS)
/// </summary>
public class UserResponseDto
{
    /// <summary>
    /// ID do usuário
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nome completo
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Email
    /// 
    /// NOTA: Estamos expondo email aqui.
    /// Se isso for sensível, remova ou crie endpoint separado
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Se o email foi confirmado
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Se o usuário está ativo
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Quando foi criado
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Último login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // NOTA: NÃO estamos expondo:
    // - PasswordHash (óbvio!)
    // - IsDeleted (implementação interna)
    // - UpdatedAt (geralmente irrelevante para cliente)
    // - CreatedBy/UpdatedBy (dados de auditoria interna)
}

/// <summary>
/// DTO para criar novo usuário
/// 
/// DIFERENÇA ENTRE Request e Response DTOs:
/// - Request: o que o cliente ENVIA
/// - Response: o que a API RETORNA
/// 
/// Geralmente são diferentes porque:
/// - Request não tem Id (é gerado pelo servidor)
/// - Request não tem timestamps (gerados automaticamente)
/// - Response pode ter campos calculados
/// </summary>
public class CreateUserRequestDto
{
    /// <summary>
    /// Nome completo do usuário
    /// 
    /// VALIDAÇÃO: 3-100 caracteres (veja CreateUserValidator)
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Email único
    /// 
    /// VALIDAÇÃO: formato de email válido
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Senha
    /// 
    /// VALIDAÇÃO: mínimo 6 caracteres (em prod, use regras mais fortes!)
    /// 
    /// SEGURANÇA:
    /// - Enviada via HTTPS apenas
    /// - Nunca logue senhas!
    /// - Será hasheada antes de salvar
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Confirmação de senha
    /// 
    /// UX: Força usuário a digitar senha duas vezes para evitar erros
    /// </summary>
    public string ConfirmPassword { get; set; }
}

/// <summary>
/// DTO para atualizar usuário
/// 
/// NOTA: Senha não está aqui!
/// Atualização de senha deve ser endpoint separado por segurança
/// (geralmente exige senha atual para confirmar)
/// </summary>
public class UpdateUserRequestDto
{
    /// <summary>
    /// Novo nome completo
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Novo email
    /// 
    /// IMPORTANTE: Se email mudar, desconfirme e envie novo email de confirmação
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// DTO para login
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

/// <summary>
/// DTO de resposta de autenticação
/// 
/// Contém tokens JWT para autenticação
/// </summary>
public class AuthenticationResponseDto
{
    /// <summary>
    /// Access Token (JWT)
    /// Curta duração (15-60 minutos)
    /// Enviado no header: Authorization: Bearer {token}
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Refresh Token
    /// Longa duração (7-30 dias)
    /// Usado para obter novo AccessToken quando expira
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// Quando o AccessToken expira
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Dados do usuário autenticado
    /// </summary>
    public UserResponseDto User { get; set; }
}

/// <summary>
/// DTO para paginação
/// 
/// SEMPRE use paginação em prod para evitar problemas de performance
/// </summary>
public class PagedRequestDto
{
    /// <summary>
    /// Número da página (começa em 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Tamanho da página
    /// 
    /// DICA: Limite máximo (ex: 100) para evitar abuso
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Validação básica
    /// </summary>
    public void Normalize()
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = 10;
        if (PageSize > 100) PageSize = 100; // Máximo
    }
}

/// <summary>
/// DTO de resposta paginada genérico
/// 
/// EXEMPLO DE USO:
/// 
/// [HttpGet]
/// public async Task<PagedResponseDto<UserResponseDto>> GetUsers([FromQuery] PagedRequestDto request)
/// {
///     var (users, total) = await _repository.GetPagedAsync(request.PageNumber, request.PageSize);
///     var dtos = users.Select(UserResponseDto.FromEntity);
///     
///     return new PagedResponseDto<UserResponseDto>
///     {
///         Items = dtos.ToList(),
///         PageNumber = request.PageNumber,
///         PageSize = request.PageSize,
///         TotalCount = total
///     };
/// }
/// </summary>
public class PagedResponseDto<T>
{
    /// <summary>
    /// Items da página atual
    /// </summary>
    public List<T> Items { get; set; }

    /// <summary>
    /// Página atual
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Tamanho da página
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total de items em todas as páginas
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total de páginas
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Se tem página anterior
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Se tem próxima página
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
