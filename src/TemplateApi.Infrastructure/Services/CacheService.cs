using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TemplateApi.Application.Common.Interfaces;

namespace TemplateApi.Infrastructure.Services;

/// <summary>
/// Implementação de Cache Service usando Redis (IDistributedCache)
/// 
/// REDIS: Cache distribuído em memória, extremamente rápido
/// - Key-value store
/// - Suporta estruturas de dados complexas
/// - Persiste dados (opcional)
/// - Pode ser clusterizado
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var cachedData = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(cachedData))
            return null;

        return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);

        var options = new DistributedCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30); // Default 30 min
        }

        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Nota: IDistributedCache não suporta pattern matching nativamente
        // Para fazer isso com Redis, precisaria usar StackExchange.Redis diretamente
        // Por simplicidade, este método não faz nada aqui
        // Em produção, considere injetar IConnectionMultiplexer do Redis
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(value);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default) where T : class
    {
        // Cache-Aside Pattern
        var cached = await GetAsync<T>(key, cancellationToken);
        
        if (cached != null)
            return cached;

        // Não está em cache, busca da fonte
        var value = await factory();
        
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}
