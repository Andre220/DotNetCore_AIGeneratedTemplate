using TemplateApi.Domain.Common;

namespace TemplateApi.Domain.Entities;

/// <summary>
/// CONCEITO: Aggregate Root - Order
/// 
/// Order é um Aggregate Root complexo que demonstra:
/// - Relacionamentos (Order -> OrderItems)
/// - Coleções encapsuladas (não expõe List diretamente)
/// - Invariants complexos
/// - Domain Events
/// - State Pattern (OrderStatus)
/// 
/// AGGREGATE:
/// Order é a raiz do agregado Order-OrderItem.
/// Isso significa:
/// - OrderItem só pode ser acessado através de Order
/// - Order garante a consistência de seus OrderItems
/// - Transações devem abranger todo o agregado
/// </summary>
public class Order : AggregateRoot
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // ENCAPSULAÇÃO: Não expõe List<> diretamente
    // Retorna IReadOnlyCollection para evitar modificações externas
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Result<Order> Create(int userId)
    {
        if (userId <= 0)
            return Result<Order>.Failure(new List<string> { "UserId inválido" });

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = 0
        };

        return Result<Order>.Success(order);
    }

    /// <summary>
    /// Adiciona item ao pedido
    /// INVARIANT: Não pode adicionar item a pedido já finalizado
    /// </summary>
    public Result AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure("Não é possível adicionar itens a um pedido que não está pendente");

        if (quantity <= 0)
            return Result.Failure("Quantidade deve ser maior que zero");

        // Verificar se já existe item com mesmo produto
        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            var updateResult = existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            if (updateResult.IsFailure)
                return updateResult;
        }
        else
        {
            var itemResult = OrderItem.Create(product.Id, product.Name, product.Price, quantity);
            if (itemResult.IsFailure)
                return Result.Failure(itemResult.Errors);

            _items.Add(itemResult.Value!);
        }

        RecalculateTotalAmount();
        return Result.Success();
    }

    public Result RemoveItem(int productId)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure("Não é possível remover itens de um pedido que não está pendente");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return Result.Failure("Item não encontrado no pedido");

        _items.Remove(item);
        RecalculateTotalAmount();
        
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure($"Pedido não pode ser confirmado. Status atual: {Status}");

        if (!_items.Any())
            return Result.Failure("Não é possível confirmar pedido sem itens");

        Status = OrderStatus.Confirmed;
        
        // Domain Event: OrderConfirmedEvent
        AddDomainEvent(new OrderConfirmedEvent(Id, UserId, TotalAmount));
        
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != OrderStatus.Confirmed && Status != OrderStatus.Processing)
            return Result.Failure($"Pedido não pode ser concluído. Status atual: {Status}");

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        
        return Result.Success();
    }

    public Result Cancel(string reason)
    {
        if (Status == OrderStatus.Completed)
            return Result.Failure("Não é possível cancelar um pedido já concluído");

        if (Status == OrderStatus.Cancelled)
            return Result.Failure("Pedido já está cancelado");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;

        return Result.Success();
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _items.Sum(i => i.Subtotal);
    }
}

/// <summary>
/// OrderItem: Entidade filha do agregado Order
/// Não pode existir sem um Order
/// </summary>
public class OrderItem : BaseEntity
{
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Subtotal => UnitPrice * Quantity;

    private OrderItem() { }

    internal static Result<OrderItem> Create(int productId, string productName, decimal unitPrice, int quantity)
    {
        var errors = new List<string>();

        if (productId <= 0)
            errors.Add("ProductId inválido");

        if (string.IsNullOrWhiteSpace(productName))
            errors.Add("Nome do produto é obrigatório");

        if (unitPrice <= 0)
            errors.Add("Preço unitário deve ser maior que zero");

        if (quantity <= 0)
            errors.Add("Quantidade deve ser maior que zero");

        if (errors.Any())
            return Result<OrderItem>.Failure(errors);

        var item = new OrderItem
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };

        return Result<OrderItem>.Success(item);
    }

    internal Result UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return Result.Failure("Quantidade deve ser maior que zero");

        Quantity = newQuantity;
        return Result.Success();
    }
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Completed = 4,
    Cancelled = 5
}

/// <summary>
/// Domain Event: Disparado quando um pedido é confirmado
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public decimal TotalAmount { get; }

    public OrderConfirmedEvent(int orderId, int userId, decimal totalAmount)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
    }
}
