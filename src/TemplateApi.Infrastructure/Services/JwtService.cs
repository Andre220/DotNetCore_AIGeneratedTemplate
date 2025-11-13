using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TemplateApi.Application.Common.Interfaces;

namespace TemplateApi.Infrastructure.Services;

/// <summary>
/// CONCEITO: JWT (JSON Web Token)
/// 
/// JWT é um padrão para criar tokens de acesso que contêm informações (claims).
/// 
/// ESTRUTURA JWT:
/// Header.Payload.Signature
/// 
/// HEADER: Tipo do token e algoritmo de criptografia
/// PAYLOAD: Claims (dados do usuário: id, email, roles, etc)
/// SIGNATURE: Assinatura criptográfica que garante autenticidade
/// 
/// FLUXO:
/// 1. Usuário faz login com email/senha
/// 2. API valida credenciais
/// 3. API gera JWT com claims do usuário
/// 4. Cliente armazena JWT (localStorage, sessionStorage, cookie)
/// 5. Cliente envia JWT em cada request (Header: Authorization: Bearer {token})
/// 6. API valida JWT e extrai claims para autorização
/// 
/// VANTAGENS:
/// ✅ Stateless (não precisa armazenar sessão no servidor)
/// ✅ Escalável (funciona em múltiplos servidores)
/// ✅ Auto-contido (contém todas as informações necessárias)
/// ✅ Funciona bem com APIs REST
/// 
/// DESVANTAGENS:
/// ❌ Não pode ser revogado facilmente (use refresh tokens)
/// ❌ Token pode ficar grande se muitos claims
/// ❌ Vulnerável a XSS se armazenado em localStorage
/// </summary>
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Configurações do appsettings.json
        _secretKey = configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey não configurada");
        _issuer = configuration["Jwt:Issuer"] ?? "TemplateApi";
        _audience = configuration["Jwt:Audience"] ?? "TemplateApi";
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");
    }

    /// <summary>
    /// Gera um JWT token para o usuário
    /// 
    /// CLAIMS PADRÃO:
    /// - sub (Subject): ID do usuário
    /// - email: Email do usuário
    /// - jti (JWT ID): ID único do token
    /// - iat (Issued At): Quando foi emitido
    /// - exp (Expiration): Quando expira
    /// 
    /// CLAIMS CUSTOMIZADOS:
    /// Você pode adicionar qualquer informação que quiser:
    /// - roles, permissions, tenant_id, etc
    /// </summary>
    public string GenerateToken(int userId, string email, Dictionary<string, string>? additionalClaims = null)
    {
        // 1. CRIAR CLAIMS (informações do usuário)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        // Adicionar claims customizados
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        // 2. CRIAR CHAVE DE SEGURANÇA
        // IMPORTANTE: SecretKey deve ter pelo menos 256 bits (32 caracteres)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. CRIAR TOKEN
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        // 4. SERIALIZAR PARA STRING
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida um JWT token e extrai os claims
    /// Retorna null se o token for inválido
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Não adicionar tolerância de tempo
            }, out _);

            return principal;
        }
        catch
        {
            return null; // Token inválido
        }
    }

    /// <summary>
    /// Extrai o UserId do token
    /// </summary>
    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        var userIdClaim = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
