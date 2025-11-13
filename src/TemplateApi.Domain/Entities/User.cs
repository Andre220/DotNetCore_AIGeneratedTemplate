using TemplateApi.Domain.Common;

namespace TemplateApi.Domain.Entities;

/// <summary>
/// CONCEITO: Entity (Entidade de Domínio)
/// 
/// Uma entidade é um objeto que tem identidade única e ciclo de vida.
/// Duas entidades são iguais se têm o mesmo Id, mesmo que suas propriedades sejam diferentes.
/// 
/// EXEMPLO:
/// User(Id=1, Name="João") == User(Id=1, Name="João Silva") → TRUE (mesmo Id)
/// User(Id=1, Name="João") == User(Id=2, Name="João") → FALSE (Ids diferentes)
/// 
/// DIFERENÇA ENTRE ENTITY E VALUE OBJECT:
/// 
/// ENTITY (User, Order, Product):
/// - Tem identidade única (Id)
/// - É mutável (pode mudar ao longo do tempo)
/// - Comparação por Id
/// - Tem ciclo de vida (criado, atualizado, deletado)
/// 
/// VALUE OBJECT (Address, Money, Email):
/// - Sem identidade própria
/// - Imutável (se mudar, cria um novo)
/// - Comparação por valor de todas as propriedades
/// - Não tem ciclo de vida próprio (pertence a uma entidade)
/// 
/// REGRAS DE NEGÓCIO:
/// Entidades devem conter suas próprias regras de negócio.
/// Não deixe a lógica de negócio espalhada pelos services.
/// </summary>
public class User : AggregateRoot
{
    /// <summary>
    /// Nome completo do usuário
    /// 
    /// VALIDAÇÃO: Entre 3 e 100 caracteres
    /// </summary>
    public string FullName { get; private set; }

    /// <summary>
    /// Email único do usuário
    /// 
    /// IMPORTANTE: 
    /// - Email deve ser único (adicionar índice único no BD)
    /// - Sempre armazene em lowercase para evitar duplicatas
    /// - Validar formato antes de criar
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Hash da senha (NUNCA armazene senha em texto puro!)
    /// 
    /// SEGURANÇA:
    /// - Use BCrypt, Argon2 ou PBKDF2 para hash
    /// - NUNCA MD5 ou SHA1 (são rápidos demais, facilitam brute force)
    /// - Adicione salt (BCrypt já faz isso automaticamente)
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Indica se o usuário confirmou o email
    /// 
    /// FLUXO:
    /// 1. Usuário se registra (EmailConfirmed = false)
    /// 2. Sistema envia email com token
    /// 3. Usuário clica no link
    /// 4. Sistema valida token e chama ConfirmEmail()
    /// </summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>
    /// Indica se o usuário está ativo
    /// 
    /// DIFERENÇA DE IsDeleted:
    /// - IsDeleted = deletado permanentemente (soft delete)
    /// - IsActive = pode ser temporariamente desativado e reativado
    /// 
    /// Exemplo: Suspender usuário por comportamento inadequado
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Último login do usuário
    /// Útil para: inatividade, estatísticas, segurança
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Construtor privado - força uso do Factory Method Create()
    /// 
    /// POR QUÊ PRIVADO?
    /// - Garante que todas as validações sejam executadas
    /// - Impede criação de entidades em estado inválido
    /// - Deixa a intenção clara (User.Create vs new User())
    /// </summary>
    private User() { }

    /// <summary>
    /// Factory Method para criar um novo usuário
    /// 
    /// FACTORY METHOD PATTERN:
    /// Método estático que cria e retorna uma instância da classe.
    /// Permite validações e lógica complexa antes de criar o objeto.
    /// 
    /// EXEMPLO DE USO:
    /// var result = User.Create("João Silva", "joao@email.com", "Senha@123");
    /// if (result.IsFailure)
    ///     return BadRequest(result.Errors);
    /// 
    /// var user = result.Data;
    /// await _repository.AddAsync(user);
    /// </summary>
    /// <param name="fullName">Nome completo (3-100 caracteres)</param>
    /// <param name="email">Email válido e único</param>
    /// <param name="password">Senha (será feito hash)</param>
    /// <returns>Result com User ou erros de validação</returns>
    public static Result<User> Create(string fullName, string email, string password)
    {
        // VALIDAÇÕES DE NEGÓCIO
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3 || fullName.Length > 100)
            errors.Add("Full name must be between 3 and 100 characters");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            errors.Add("Invalid email format");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            errors.Add("Password must be at least 6 characters");

        if (errors.Any())
            return Result<User>.Failure(errors);

        // CRIAR ENTIDADE
        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim().ToLowerInvariant(), // Sempre lowercase!
            PasswordHash = HashPassword(password), // TODO: Implementar BCrypt
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // DISPARAR EVENTO DE DOMÍNIO
        // Outros handlers podem reagir (enviar email de boas-vindas, criar perfil, etc)
        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email, user.FullName));

        return Result<User>.Success(user);
    }

    /// <summary>
    /// Atualiza informações do usuário
    /// 
    /// IMPORTANTE: Métodos públicos que modificam estado devem validar!
    /// </summary>
    public Result UpdateInfo(string fullName, string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
            errors.Add("Full name must be at least 3 characters");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            errors.Add("Invalid email format");

        if (errors.Any())
            return Result.Failure(errors);

        FullName = fullName.Trim();
        
        // Se email mudou, desconfirmar
        if (Email != email.Trim().ToLowerInvariant())
        {
            Email = email.Trim().ToLowerInvariant();
            EmailConfirmed = false;
            AddDomainEvent(new UserEmailChangedEvent(Id, Email));
        }

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Confirma o email do usuário
    /// </summary>
    public void ConfirmEmail()
    {
        if (!EmailConfirmed)
        {
            EmailConfirmed = true;
            MarkAsUpdated();
            AddDomainEvent(new UserEmailConfirmedEvent(Id, Email));
        }
    }

    /// <summary>
    /// Registra login do usuário
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Desativa o usuário
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reativa o usuário
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// TODO: Implementar hash real com BCrypt
    /// Exemplo: BCrypt.Net.BCrypt.HashPassword(password)
    /// </summary>
    private static string HashPassword(string password)
    {
        // TEMPORÁRIO - Use BCrypt em produção!
        return $"HASHED_{password}";
    }
}

// DOMAIN EVENTS para o User
public class UserCreatedEvent : DomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public string FullName { get; }

    public UserCreatedEvent(int userId, string email, string fullName)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
    }
}

public class UserEmailChangedEvent : DomainEvent
{
    public int UserId { get; }
    public string NewEmail { get; }

    public UserEmailChangedEvent(int userId, string newEmail)
    {
        UserId = userId;
        NewEmail = newEmail;
    }
}

public class UserEmailConfirmedEvent : DomainEvent
{
    public int UserId { get; }
    public string Email { get; }

    public UserEmailConfirmedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}
