namespace TemplateApi.Domain.Common;

/// <summary>
/// CONCEITO: Result Pattern
/// 
/// O Result Pattern é uma alternativa ao uso de exceptions para fluxos de negócio esperados.
/// Ao invés de lançar uma exception quando algo "dá errado" (mas é esperado), retornamos
/// um objeto Result que encapsula sucesso ou falha.
/// 
/// BENEFÍCIOS:
/// 1. Performance - Exceptions são caras, Results são simples objetos
/// 2. Explícito - O retorno deixa claro que pode falhar
/// 3. Composição - Fácil encadear operações e tratar erros
/// 4. Tipo seguro - O compilador força tratamento de erros
/// 
/// QUANDO USAR:
/// - Validações de negócio (usuário não encontrado, saldo insuficiente, etc)
/// - Operações que podem falhar de forma previsível
/// 
/// QUANDO NÃO USAR:
/// - Erros inesperados do sistema (falha de BD, out of memory, etc) → usar exceptions
/// </summary>
public class Result
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Indica se a operação falhou (inverso de IsSuccess)
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Lista de mensagens de erro quando IsFailure = true
    /// </summary>
    public List<string> Errors { get; protected set; }

    /// <summary>
    /// Construtor protegido - force uso dos métodos estáticos Success() e Failure()
    /// Isso garante consistência na criação de Results
    /// </summary>
    protected Result(bool isSuccess, List<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Cria um Result de sucesso
    /// </summary>
    public static Result Success() => new Result(true, new List<string>());

    /// <summary>
    /// Cria um Result de falha com um erro
    /// </summary>
    public static Result Failure(string error) => new Result(false, new List<string> { error });

    /// <summary>
    /// Cria um Result de falha com múltiplos erros
    /// </summary>
    public static Result Failure(List<string> errors) => new Result(false, errors);
}

/// <summary>
/// Result genérico que carrega um valor quando bem-sucedido
/// 
/// EXEMPLO DE USO:
/// 
/// public Result<User> GetUser(int id)
/// {
///     var user = _repository.FindById(id);
///     
///     if (user == null)
///         return Result<User>.Failure("User not found");
///     
///     return Result<User>.Success(user);
/// }
/// 
/// // No controller:
/// var result = _service.GetUser(id);
/// if (result.IsFailure)
///     return NotFound(result.Errors);
/// 
/// return Ok(result.Data);
/// </summary>
public class Result<T> : Result
{
    /// <summary>
    /// Dados retornados quando a operação é bem-sucedida
    /// </summary>
    public T? Data { get; private set; }
    
    /// <summary>
    /// Alias para Data (para compatibilidade)
    /// </summary>
    public T? Value => Data;

    private Result(bool isSuccess, T? data, List<string> errors) : base(isSuccess, errors)
    {
        Data = data;
    }

    /// <summary>
    /// Cria um Result de sucesso com dados
    /// </summary>
    public static Result<T> Success(T data) => new Result<T>(true, data, new List<string>());

    /// <summary>
    /// Cria um Result de falha sem dados
    /// </summary>
    public new static Result<T> Failure(string error) => new Result<T>(false, default(T), new List<string> { error });

    /// <summary>
    /// Cria um Result de falha com múltiplos erros
    /// </summary>
    public new static Result<T> Failure(List<string> errors) => new Result<T>(false, default(T), errors);
}
