namespace TemplateApi.Domain.Common;

/// <summary>
/// CONCEITO: Domain Events
/// 
/// Domain Events representam algo que ACONTECEU no domínio que outras partes
/// do sistema podem querer saber.
/// 
/// EXEMPLOS:
/// - UserCreatedEvent → disparar email de boas-vindas
/// - OrderPlacedEvent → iniciar processo de pagamento, reduzir estoque
/// - PaymentReceivedEvent → liberar produto, enviar nota fiscal
/// 
/// BENEFÍCIOS:
/// 1. Desacoplamento - quem cria o evento não precisa saber quem vai reagir
/// 2. Single Responsibility - cada handler faz uma coisa
/// 3. Extensibilidade - adicionar novos comportamentos sem modificar código existente
/// 4. Auditoria - histórico completo do que aconteceu
/// 
/// QUANDO USAR:
/// - Ações que devem acontecer DEPOIS de algo (side effects)
/// - Notificar outras partes do sistema
/// - Integração entre bounded contexts
/// 
/// PATTERN: Observer Pattern / Event-Driven Architecture
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Identificador único do evento
    /// Útil para idempotência e rastreamento
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Quando o evento ocorreu
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Tipo do evento (nome da classe)
    /// Útil para logging e debugging
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Implementação base para Domain Events
/// 
/// EXEMPLO DE USO:
/// 
/// public class UserCreatedEvent : DomainEvent
/// {
///     public int UserId { get; }
///     public string Email { get; }
///     
///     public UserCreatedEvent(int userId, string email)
///     {
///         UserId = userId;
///         Email = email;
///     }
/// }
/// 
/// // Ao criar usuário:
/// var user = User.Create(name, email);
/// user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Email));
/// 
/// // Ao salvar:
/// await _unitOfWork.SaveChangesAsync(); // Dispara eventos automaticamente
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }
    public string EventType => GetType().Name;

    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Entidade que suporta Domain Events
/// 
/// Estende BaseEntity adicionando capacidade de coletar e disparar eventos
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    /// <summary>
    /// Lista de eventos de domínio pendentes
    /// Serão disparados quando SaveChanges for chamado
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Eventos de domínio desta entidade (read-only)
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adiciona um evento de domínio
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Remove um evento de domínio
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Limpa todos os eventos
    /// Chamado automaticamente após disparar os eventos
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
