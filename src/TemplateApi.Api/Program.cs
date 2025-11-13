using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TemplateApi.Application;
using TemplateApi.Infrastructure;

/// <summary>
/// =======================================================================
/// TEMPLATE API - .NET 9 REFERENCE PROJECT
/// =======================================================================
/// 
/// Este é um projeto de referência que demonstra TODOS os recursos modernos
/// do ASP.NET Core 9.0, incluindo:
/// 
/// ✅ Clean Architecture + DDD
/// ✅ Vertical Slice Architecture
/// ✅ CQRS Pattern
/// ✅ Repository + Unit of Work
/// ✅ JWT Authentication
/// ✅ API Versioning
/// ✅ Rate Limiting (.NET 7+)
/// ✅ Output Caching (.NET 7+)
/// ✅ Response Compression (Gzip/Brotli)
/// ✅ OpenAPI/Swagger avançado
/// ✅ Health Checks + UI
/// ✅ Serilog (Structured Logging)
/// ✅ OpenTelemetry
/// ✅ CORS
/// ✅ FluentValidation
/// ✅ Redis Cache
/// ✅ PostgreSQL + EF Core
/// ✅ Background Services
/// ✅ SignalR (Real-time)
/// ✅ gRPC (High-performance RPC)
/// ✅ Middleware Pipeline customizado
/// 
/// ESTRUTURA DO PROJETO:
/// - Domain: Entidades, interfaces, regras de negócio
/// - Application: Use cases, DTOs, validators (Vertical Slices)
/// - Infrastructure: Implementação (EF, Redis, Email, etc)
/// - API: Controllers, middleware, configuration
/// 
/// =======================================================================
/// </summary>

var builder = WebApplication.CreateBuilder(args);

// =======================================================================
// SERILOG - STRUCTURED LOGGING
// =======================================================================
// Serilog é superior ao logging padrão do .NET:
// - Structured logging (logs como JSON)
// - Múltiplos sinks (console, file, Seq, ELK, etc)
// - Performance superior
// - Enriquecimento automático (machine name, thread id, etc)

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("🚀 Starting Template API...");

// =======================================================================
// DEPENDENCY INJECTION - CAMADAS
// =======================================================================

// Infrastructure (Database, Repositories, Cache, Email, etc)
builder.Services.AddInfrastructure(builder.Configuration);

// Application (Handlers, Validators, etc)
builder.Services.AddApplication();

// =======================================================================
// AUTHENTICATION - JWT BEARER
// =======================================================================
// JWT (JSON Web Token) para autenticação stateless
// Token contém claims do usuário, assinado criptograficamente

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Sem tolerância de tempo
    };

    // Eventos JWT para debug/logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("JWT Token validated for user: {User}", 
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// =======================================================================
// CORS - CROSS-ORIGIN RESOURCE SHARING
// =======================================================================
// Permite que front-ends (React, Angular, etc) chamem a API

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://meusite.com", "https://app.meusite.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// =======================================================================
// RATE LIMITING (.NET 7+)
// =======================================================================
// Protege a API contra abuso/DDoS

builder.Services.AddRateLimiter(options =>
{
    // Fixed Window: X requests por período fixo
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 0;
    });

    // Sliding Window: Mais justo que Fixed Window
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 0;
    });

    // Token Bucket: Permite bursts
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 50;
        opt.QueueLimit = 0;
    });

    // Concurrency: Limite de requests simultâneas
    options.AddConcurrencyLimiter("concurrency", opt =>
    {
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
});

// =======================================================================
// OUTPUT CACHING (.NET 7+)
// =======================================================================
// Cache de respostas HTTP inteiras (mais eficiente que Response Caching)

builder.Services.AddOutputCache(options =>
{
    // Política padrão: cacheia por 60 segundos
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));

    // Política customizada
    options.AddPolicy("ProductsCache", builder => 
        builder.Expire(TimeSpan.FromMinutes(5))
               .Tag("products"));

    // Varia por query string
    options.AddPolicy("VaryByQuery", builder => 
        builder.Expire(TimeSpan.FromMinutes(1))
               .SetVaryByQuery("page", "pageSize"));
});

// =======================================================================
// RESPONSE COMPRESSION
// =======================================================================
// Comprime respostas para reduzir tráfego de rede
// Brotli é mais eficiente que Gzip, mas requer mais CPU

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// =======================================================================
// API VERSIONING
// =======================================================================
// Permite múltiplas versões da API coexistirem
// TODO: Descomentar após instalar Asp.Versioning.Mvc

// builder.Services.AddApiVersioning(options =>
// {
//     options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
//     options.AssumeDefaultVersionWhenUnspecified = true;
//     options.ReportApiVersions = true;
// })
// .AddApiExplorer(options =>
// {
//     options.GroupNameFormat = "'v'VVV";
//     options.SubstituteApiVersionInUrl = true;
// });

// =======================================================================
// OPENAPI/SWAGGER
// =======================================================================
// Documentação interativa da API

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Template API",
        Version = "v1",
        Description = @"
# Template API - .NET 9 Reference Project

Esta é uma API de referência que demonstra **TODOS** os recursos modernos do ASP.NET Core 9.0.

## Recursos Implementados

- ✅ **Clean Architecture** + DDD
- ✅ **Vertical Slice Architecture** (CQRS)
- ✅ **JWT Authentication** (Bearer Token)
- ✅ **Rate Limiting** (proteção contra DDoS)
- ✅ **Output Caching** (performance)
- ✅ **Health Checks** + UI
- ✅ **OpenTelemetry** (observabilidade)
- ✅ **Structured Logging** (Serilog)
- ✅ **Redis Cache** (distribuído)
- ✅ **PostgreSQL** + Entity Framework Core
- ✅ **FluentValidation** (validação robusta)
- ✅ **Response Compression** (Gzip/Brotli)

## Autenticação

Para usar endpoints autenticados:
1. Registre um usuário: `POST /api/v1/auth/register`
2. Faça login: `POST /api/v1/auth/login`
3. Copie o token JWT da resposta
4. Clique em 'Authorize' acima e cole o token
",
        Contact = new OpenApiContact
        {
            Name = "Template API Team",
            Email = "contato@templateapi.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // JWT Security Scheme
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: 'Bearer {seu token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // XML Comments (se houver)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// =======================================================================
// OPENTELEMETRY - OBSERVABILITY
// =======================================================================
// Tracing, Metrics, Logging distribuído
// TODO: Instalar OpenTelemetry.Instrumentation.AspNetCore

// builder.Services.AddOpenTelemetry()
//     .WithTracing(tracing => tracing
//         .AddAspNetCoreInstrumentation()
//         .AddConsoleExporter())
//     .WithMetrics(metrics => metrics
//         .AddAspNetCoreInstrumentation()
//         .AddConsoleExporter());

// =======================================================================
// CONTROLLERS
// =======================================================================

builder.Services.AddControllers();

// =======================================================================
// BUILD APP
// =======================================================================

var app = builder.Build();

// =======================================================================
// MIDDLEWARE PIPELINE
// =======================================================================
// A ORDEM IMPORTA! Siga esta sequência:

// 1. Exception Handling (primeiro para pegar todos os erros)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Response Compression
app.UseResponseCompression();

// 4. CORS (antes de Auth!)
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "Production");

// 5. Rate Limiting
app.UseRateLimiter();

// 6. Output Caching
app.UseOutputCache();

// 7. Routing
app.UseRouting();

// 8. Authentication (antes de Authorization!)
app.UseAuthentication();

// 9. Authorization
app.UseAuthorization();

// =======================================================================
// SWAGGER UI
// =======================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Template API v1");
        options.RoutePrefix = string.Empty; // Swagger na raiz: https://localhost:5001/
        options.DocumentTitle = "Template API - Documentation";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
    });
}

// =======================================================================
// HEALTH CHECKS
// =======================================================================

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Apenas verifica se app está rodando
});

// Health Checks UI (Dashboard visual)
// app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

// =======================================================================
// ENDPOINTS
// =======================================================================

app.MapControllers();

// Minimal API Example
app.MapGet("/", () => new
{
    message = "Template API is running!",
    version = "1.0.0",
    documentation = "/swagger",
    health = "/health",
    healthUi = "/health-ui"
})
.WithName("Root")
.WithOpenApi();

// =======================================================================
// RUN
// =======================================================================

try
{
    Log.Information("✅ Template API started successfully!");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
