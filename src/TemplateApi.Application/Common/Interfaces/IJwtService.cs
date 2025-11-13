using System.Security.Claims;

namespace TemplateApi.Application.Common.Interfaces;

/// <summary>
/// CONCEITO: JWT Service Interface
/// 
/// Serviço para geração e validação de JWT tokens
/// Usado para autenticação baseada em tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gera um JWT token para o usuário
    /// </summary>
    string GenerateToken(int userId, string email, Dictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Valida um JWT token e retorna os claims
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extrai o User ID do token
    /// </summary>
    int? GetUserIdFromToken(string token);
}
