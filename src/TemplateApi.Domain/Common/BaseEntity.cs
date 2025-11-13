namespace TemplateApi.Domain.Common;

/// <summary>
/// CONCEITO: Base Entity
/// 
/// Uma classe base para todas as entidades do domínio.
/// Centraliza propriedades e comportamentos comuns a todas as entidades.
/// 
/// PROPRIEDADES COMUNS:
/// - Id: Identificador único
/// - CreatedAt: Quando foi criado
/// - UpdatedAt: Quando foi atualizado pela última vez
/// - IsDeleted: Soft delete (exclusão lógica)
/// 
/// BENEFÍCIOS:
/// 1. DRY (Don't Repeat Yourself) - não repetir essas props em cada entidade
/// 2. Audit Trail - rastrear quando e por quem algo foi criado/modificado
/// 3. Soft Delete - não deletar fisicamente do BD, apenas marcar como deletado
/// 4. Tipo seguro - Id sempre int (ou pode ser Guid)
/// 
/// SOFT DELETE:
/// Ao invés de deletar fisicamente (DELETE FROM), apenas marca IsDeleted = true.
/// Benefícios:
/// - Pode "desfazer" uma exclusão
/// - Mantém histórico completo
/// - Evita quebrar relacionamentos
/// - Pode auditar exclusões
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Identificador único da entidade
    /// 
    /// DICA: Pode trocar para Guid se preferir IDs globalmente únicos
    /// int: mais rápido, menor, sequencial
    /// Guid: único globalmente, pode gerar antes de salvar no BD
    /// </summary>
    public int Id { get; protected set; }

    /// <summary>
    /// Data e hora de criação da entidade
    /// 
    /// IMPORTANTE: Use DateTime.UtcNow para evitar problemas de timezone
    /// Sempre armazene em UTC no banco e converta para timezone local na apresentação
    /// </summary>
    public DateTime CreatedAt { get; internal set; }

    /// <summary>
    /// Data e hora da última atualização
    /// 
    /// Útil para:
    /// - Rastreamento de mudanças
    /// - Sincronização (buscar apenas o que mudou desde X)
    /// - Cache invalidation
    /// </summary>
    public DateTime? UpdatedAt { get; internal set; }

    /// <summary>
    /// Indica se a entidade foi "deletada" (exclusão lógica)
    /// 
    /// IMPORTANTE: Configure seus queries para SEMPRE filtrar IsDeleted = false
    /// Exemplo no EF Core: modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Quem criou esta entidade (opcional)
    /// 
    /// DICA: Capture do token JWT (ClaimTypes.NameIdentifier)
    /// </summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>
    /// Quem atualizou pela última vez (opcional)
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Construtor protegido - apenas classes derivadas podem instanciar
    /// Força a criação de entidades através de métodos nomeados (Named Constructors)
    /// ou Factory Methods, o que deixa a intenção mais clara
    /// </summary>
    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca a entidade como atualizada
    /// 
    /// CHAMADA AUTOMÁTICA: Configure no EF Core SaveChangesAsync para chamar
    /// automaticamente antes de salvar
    /// </summary>
    public virtual void MarkAsUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Soft delete - marca como deletado sem remover do banco
    /// 
    /// NUNCA faça: dbContext.Users.Remove(user);
    /// SEMPRE faça: user.MarkAsDeleted(); await dbContext.SaveChangesAsync();
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// "Desfaz" uma exclusão
    /// 
    /// Útil para funcionalidade de "restaurar" items deletados
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
