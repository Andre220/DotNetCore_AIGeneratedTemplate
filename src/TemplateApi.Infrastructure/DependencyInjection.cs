using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplateApi.Application.Common.Interfaces;
using TemplateApi.Domain.Interfaces;
using TemplateApi.Infrastructure.Persistence;
using TemplateApi.Infrastructure.Persistence.Repositories;
using TemplateApi.Infrastructure.Services;

namespace TemplateApi.Infrastructure;

/// <summary>
/// CONCEITO: Dependency Injection Configuration
/// 
/// Este arquivo centraliza a configuração de DI da camada de Infrastructure.
/// Registra todos os serviços, repositórios, DbContext, etc.
/// 
/// EXTENSÃO METHOD PATTERN:
/// Ao invés de poluir Program.cs com dezenas de AddScoped/AddSingleton,
/// criamos métodos de extensão que encapsulam a configuração de cada camada.
/// 
/// LIFETIMES:
/// 
/// TRANSIENT (AddTransient):
/// - Nova instância a cada injeção
/// - Use para: serviços leves, stateless, sem recursos compartilhados
/// - Exemplo: DateTimeService, validators
/// 
/// SCOPED (AddScoped):
/// - Uma instância por request HTTP
/// - Use para: DbContext, repositórios, Unit of Work
/// - Exemplo: ApplicationDbContext, UserRepository
/// - Importante: Vida útil limitada ao request, liberado automaticamente
/// 
/// SINGLETON (AddSingleton):
/// - Uma única instância para toda a aplicação
/// - Use para: caches, configurações, serviços pesados
/// - CUIDADO: Deve ser thread-safe!
/// - Exemplo: IConfiguration, cache services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona todos os serviços de Infrastructure
    /// Chamado em Program.cs: builder.Services.AddInfrastructure(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DATABASE (Entity Framework Core + PostgreSQL)
        services.AddDatabase(configuration);

        // REPOSITORIES & UNIT OF WORK
        services.AddRepositories();

        // CACHING (Redis)
        services.AddCaching(configuration);

        // INFRASTRUCTURE SERVICES
        services.AddInfrastructureServices(configuration);

        // HEALTH CHECKS
        // Comentado porque falta pacote: Microsoft.Extensions.Diagnostics.HealthChecks
        // services.AddInfrastructureHealthChecks(configuration);

        return services;
    }

    /// <summary>
    /// Configura o banco de dados (PostgreSQL com EF Core)
    /// </summary>
    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // RESILIENCE: Retry automático em caso de falha transitória
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);

                // PERFORMANCE: Command timeout
                npgsqlOptions.CommandTimeout(30);

                // MIGRATIONS: Assembly onde estão as migrations
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

            // DESENVOLVIMENTO: Log de queries SQL
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }

    /// <summary>
    /// Registra repositórios e Unit of Work
    /// SCOPED: Uma instância por request HTTP
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specific Repositories
        services.AddScoped<IUserRepository, UserRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    /// <summary>
    /// Configura cache distribuído (Redis)
    /// </summary>
    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnection))
        {
            // REDIS: Cache distribuído
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "TemplateApi:";
            });
        }
        else
        {
            // FALLBACK: Cache em memória (não recomendado para produção com múltiplas instâncias)
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ICacheService, CacheService>();

        // IN-MEMORY CACHE: Para dados que não precisam ser compartilhados entre instâncias
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Registra serviços de infraestrutura
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Password Hasher
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // DateTime Service
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // HTTP Client Factory (para chamadas a APIs externas)
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Configura Health Checks
    /// Monitora saúde do sistema: BD, Redis, APIs externas, etc
    /// 
    /// NOTA: Configuração comentada porque faltam os pacotes NuGet:
    /// - Microsoft.Extensions.Diagnostics.HealthChecks (básico)
    /// - AspNetCore.HealthChecks.Npgsql (PostgreSQL)
    /// - AspNetCore.HealthChecks.Redis (Redis)
    /// - AspNetCore.HealthChecks.UI (Dashboard UI)
    /// 
    /// Para adicionar:
    /// dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
    /// dotnet add package AspNetCore.HealthChecks.Npgsql
    /// dotnet add package AspNetCore.HealthChecks.Redis
    /// dotnet add package AspNetCore.HealthChecks.UI
    /// </summary>
    private static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // BASIC HEALTH CHECK (requer pacote Microsoft.Extensions.Diagnostics.HealthChecks)
        // var healthChecksBuilder = services.AddHealthChecks();

        // Database Health Check
        // NOTA: Requer pacote AspNetCore.HealthChecks.Npgsql
        // var dbConnection = configuration.GetConnectionString("DefaultConnection");
        // if (!string.IsNullOrEmpty(dbConnection))
        // {
        //     healthChecksBuilder.AddNpgSql(
        //         dbConnection,
        //         name: "database",
        //         tags: new[] { "db", "postgresql" });
        // }

        // Redis Health Check
        // NOTA: Requer pacote AspNetCore.HealthChecks.Redis
        // var redisConnection = configuration.GetConnectionString("Redis");
        // if (!string.IsNullOrEmpty(redisConnection))
        // {
        //     healthChecksBuilder.AddRedis(
        //         redisConnection,
        //         name: "redis",
        //         tags: new[] { "cache", "redis" });
        // }

        // Health Checks UI (Dashboard)
        // NOTA: Requer pacote AspNetCore.HealthChecks.UI
        // services.AddHealthChecksUI(setup =>
        // {
        //     setup.SetEvaluationTimeInSeconds(30); // Verifica a cada 30 segundos
        //     setup.MaximumHistoryEntriesPerEndpoint(50);
        // }).AddInMemoryStorage();

        return services;
    }
}
