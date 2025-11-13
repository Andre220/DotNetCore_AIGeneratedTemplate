namespace TemplateApi.Application.Common.Interfaces;

/// <summary>
/// CONCEITO: Cache Service
/// 
/// Cache é essencial para performance em aplicações modernas.
/// Armazena dados frequentemente acessados em memória para acesso rápido.
/// 
/// QUANDO USAR CACHE:
/// 1. Dados que mudam pouco (configurações, listas de países, etc)
/// 2. Dados caros de buscar (queries complexas, APIs externas)
/// 3. Dados acessados frequentemente
/// 
/// QUANDO NÃO USAR:
/// 1. Dados que mudam muito
/// 2. Dados sensíveis/pessoais (por questões de segurança)
/// 3. Dados únicos por usuário (a menos que seja cache distribuído)
/// 
/// TIPOS DE CACHE:
/// 
/// 1. IN-MEMORY (IMemoryCache)
///    - Mais rápido
///    - Limitado à memória do servidor
///    - Perdido ao reiniciar
///    - Não compartilhado entre instâncias
///    → Usar para: dados temporários, sessão única
/// 
/// 2. DISTRIBUTED (Redis, SQL Server)
///    - Mais lento que memória (mas ainda rápido)
///    - Compartilhado entre instâncias
///    - Persiste entre restarts
///    - Pode ser clusterizado
///    → Usar para: produção com múltiplos servidores
/// 
/// ESTRATÉGIAS DE CACHE:
/// 
/// 1. CACHE-ASIDE (Lazy Loading)
///    - Tenta buscar do cache
///    - Se não tem, busca da fonte
///    - Adiciona no cache
///    → Mais comum
/// 
/// 2. WRITE-THROUGH
///    - Escreve no cache E na fonte ao mesmo tempo
///    → Garante consistência
/// 
/// 3. WRITE-BEHIND
///    - Escreve no cache imediatamente
///    - Escreve na fonte depois (async)
///    → Máxima performance, risco de perda
/// 
/// EXEMPLO DE USO (Cache-Aside):
/// 
/// public async Task<Product> GetProductAsync(int id)
/// {
///     // 1. Tenta do cache
///     var cached = await _cache.GetAsync<Product>($"product:{id}");
///     if (cached != null)
///         return cached;
///     
///     // 2. Busca da fonte
///     var product = await _repository.GetByIdAsync(id);
///     
///     // 3. Adiciona no cache
///     await _cache.SetAsync($"product:{id}", product, TimeSpan.FromMinutes(10));
///     
///     return product;
/// }
/// 
/// INVALIDAÇÃO DE CACHE:
/// 
/// Cache invalidation é um dos problemas mais difíceis em computação!
/// 
/// Estratégias:
/// 1. TTL (Time To Live) - expira após X tempo
/// 2. Invalidação explícita - remove quando dados mudam
/// 3. Event-based - escuta eventos de mudança
/// 
/// Exemplo:
/// public async Task UpdateProductAsync(Product product)
/// {
///     await _repository.UpdateAsync(product);
///     await _cache.RemoveAsync($"product:{product.Id}"); // Invalidate!
/// }
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Busca valor do cache
    /// </summary>
    /// <typeparam name="T">Tipo do valor</typeparam>
    /// <param name="key">Chave única</param>
    /// <returns>Valor ou null se não existir/expirou</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Adiciona ou atualiza valor no cache
    /// </summary>
    /// <param name="key">Chave única</param>
    /// <param name="value">Valor a cachear</param>
    /// <param name="expiration">Tempo até expirar (TTL)</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove do cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove múltiplas chaves (pattern matching)
    /// 
    /// Exemplo: RemoveByPatternAsync("user:*") remove todas chaves de usuários
    /// 
    /// CUIDADO: Pode ser lento com Redis se houver muitas chaves
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se chave existe
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca ou cria (pattern comum)
    /// 
    /// EXEMPLO:
    /// var product = await _cache.GetOrCreateAsync(
    ///     $"product:{id}",
    ///     async () => await _repository.GetByIdAsync(id),
    ///     TimeSpan.FromMinutes(10)
    /// );
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// CONCEITO: Logging Service
/// 
/// Logging é ESSENCIAL para:
/// 1. Debug de problemas em produção
/// 2. Auditoria de ações
/// 3. Monitoramento de performance
/// 4. Análise de comportamento
/// 
/// LOG LEVELS (em ordem de severidade):
/// 
/// TRACE - Detalhes extremos (cada linha de código)
///   → Usar apenas em debug local
/// 
/// DEBUG - Informações de debug (valores de variáveis, fluxo)
///   → Usar em desenvolvimento
/// 
/// INFORMATION - Eventos normais do sistema (user logged in, order created)
///   → Usar em produção (moderadamente)
/// 
/// WARNING - Algo errado mas não crítico (retry, deprecated API)
///   → Sempre revisar
/// 
/// ERROR - Erro que afeta operação mas sistema continua
///   → Alerta! Precisa correção
/// 
/// CRITICAL - Sistema está falhando, precisa atenção imediata!
///   → Página o time imediatamente!
/// 
/// ESTRUTURED LOGGING:
/// 
/// ❌ String formatting (antigo):
/// _logger.LogInformation($"User {userId} placed order {orderId}");
/// 
/// ✅ Structured (moderno):
/// _logger.LogInformation("User {UserId} placed order {OrderId}", userId, orderId);
/// 
/// Benefícios estruturado:
/// - Pode buscar por UserId específico
/// - Ferramentas como ELK, Seq podem indexar
/// - Performance (não concatena strings)
/// 
/// EXEMPLO DE USO:
/// 
/// public class CreateUserHandler
/// {
///     private readonly ILogger _logger;
///     
///     public async Task<Result> Handle(CreateUserCommand cmd)
///     {
///         _logger.LogInformation("Creating user with email {Email}", cmd.Email);
///         
///         try
///         {
///             var user = await _service.CreateAsync(cmd);
///             _logger.LogInformation("User {UserId} created successfully", user.Id);
///             return Result.Success(user);
///         }
///         catch (Exception ex)
///         {
///             _logger.LogError(ex, "Failed to create user with email {Email}", cmd.Email);
///             return Result.Failure("Failed to create user");
///         }
///     }
/// }
/// </summary>
public interface ILogService
{
    void LogTrace(string message, params object[] args);
    void LogDebug(string message, params object[] args);
    void LogInformation(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
    void LogCritical(Exception exception, string message, params object[] args);
}

/// <summary>
/// CONCEITO: Email/Notification Service
/// 
/// Serviço para enviar notificações (email, SMS, push)
/// 
/// IMPORTANTE:
/// - Nunca envie diretamente do handler (é lento!)
/// - Use fila/background job
/// - Tenha retry logic (email pode falhar)
/// - Template emails (não hardcode HTML)
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia email
    /// </summary>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email com template
    /// </summary>
    Task SendTemplatedEmailAsync(string to, string templateName, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de confirmação
    /// </summary>
    Task SendConfirmationEmailAsync(string to, string confirmationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de reset de senha
    /// </summary>
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
}

/// <summary>
/// CONCEITO: DateTime Service
/// 
/// POR QUÊ ABSTRAIR DateTime.Now?
/// 
/// 1. TESTABILIDADE
///    - Não consigo testar código que depende de DateTime.Now
///    - Com IDateTimeService, posso mockar e controlar o tempo
/// 
/// 2. TIMEZONE
///    - Sempre use UTC internamente
///    - Converta para timezone local apenas na apresentação
/// 
/// EXEMPLO SEM ABSTRAÇÃO (difícil de testar):
/// 
/// public bool IsExpired()
/// {
///     return ExpiresAt < DateTime.Now; // Como testar isso?
/// }
/// 
/// EXEMPLO COM ABSTRAÇÃO (fácil de testar):
/// 
/// public bool IsExpired(IDateTimeService dateTime)
/// {
///     return ExpiresAt < dateTime.UtcNow;
/// }
/// 
/// // No teste:
/// var mockDateTime = Mock.Of<IDateTimeService>(d => d.UtcNow == new DateTime(2025, 1, 1));
/// var result = entity.IsExpired(mockDateTime); // Controlado!
/// </summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
    DateOnly Today { get; }
}
