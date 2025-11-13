using Microsoft.Extensions.DependencyInjection;
using TemplateApi.Application.Features.Auth.Login;
using TemplateApi.Application.Features.Auth.Register;
using TemplateApi.Application.Features.Users.GetAllUsers;
using TemplateApi.Application.Features.Users.GetUserById;

namespace TemplateApi.Application;

/// <summary>
/// Configuração de Dependency Injection da camada Application
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // HANDLERS - Vertical Slices
        // Cada handler é registrado como Scoped (uma instância por request)

        // Auth Handlers
        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginCommandHandler>();

        // User Handlers
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetAllUsersQueryHandler>();

        // FluentValidation - já configurado via pacote
        // Validators são descobertos automaticamente se usar FluentValidation.AspNetCore

        return services;
    }
}
