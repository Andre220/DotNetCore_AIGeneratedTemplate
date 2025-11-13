using TemplateApi.Domain.Common;

namespace TemplateApi.Domain.Entities;

/// <summary>
/// CONCEITO: Aggregate Root - Product
/// 
/// Product é um Aggregate Root que representa um produto no catálogo.
/// Demonstra conceitos como:
/// - Value Objects (Money)
/// - Enums
/// - Invariants (regras de negócio)
/// </summary>
public class Product : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string Sku { get; private set; } // Stock Keeping Unit
    public ProductCategory Category { get; private set; }
    public bool IsAvailable { get; private set; }

    private Product() { }

    public static Result<Product> Create(
        string name, 
        string description, 
        decimal price, 
        int stockQuantity, 
        string sku,
        ProductCategory category)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 200)
            errors.Add("Nome deve ter entre 3 e 200 caracteres");

        if (price <= 0)
            errors.Add("Preço deve ser maior que zero");

        if (stockQuantity < 0)
            errors.Add("Quantidade em estoque não pode ser negativa");

        if (string.IsNullOrWhiteSpace(sku))
            errors.Add("SKU é obrigatório");

        if (errors.Any())
            return Result<Product>.Failure(errors);

        var product = new Product
        {
            Name = name,
            Description = description ?? string.Empty,
            Price = price,
            StockQuantity = stockQuantity,
            Sku = sku.ToUpperInvariant(),
            Category = category,
            IsAvailable = stockQuantity > 0
        };

        return Result<Product>.Success(product);
    }

    public Result DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantidade deve ser maior que zero");

        if (StockQuantity < quantity)
            return Result.Failure($"Estoque insuficiente. Disponível: {StockQuantity}, Solicitado: {quantity}");

        StockQuantity -= quantity;
        
        if (StockQuantity == 0)
            IsAvailable = false;

        return Result.Success();
    }

    public Result IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantidade deve ser maior que zero");

        StockQuantity += quantity;
        IsAvailable = true;

        return Result.Success();
    }

    public Result UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            return Result.Failure("Preço deve ser maior que zero");

        Price = newPrice;
        return Result.Success();
    }

    public void UpdateInfo(string name, string description)
    {
        if (!string.IsNullOrWhiteSpace(name) && name.Length >= 3 && name.Length <= 200)
            Name = name;

        Description = description ?? string.Empty;
    }
}

/// <summary>
/// Categoria do produto - demonstra uso de Enums
/// </summary>
public enum ProductCategory
{
    Electronics = 1,
    Clothing = 2,
    Books = 3,
    Food = 4,
    HomeAndGarden = 5,
    Sports = 6,
    Toys = 7,
    Other = 99
}
