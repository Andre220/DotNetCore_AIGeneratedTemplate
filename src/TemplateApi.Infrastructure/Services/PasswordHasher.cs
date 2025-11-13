using BCrypt.Net;
using TemplateApi.Application.Common.Interfaces;

namespace TemplateApi.Infrastructure.Services;

/// <summary>
/// CONCEITO: Password Hashing com BCrypt
/// 
/// NUNCA armazene senhas em texto puro!
/// 
/// HASHING vs ENCRYPTION:
/// 
/// ENCRYPTION (criptografia):
/// - Reversível (pode descriptografar)
/// - Usa chave secreta
/// - Exemplo: AES, RSA
/// - Uso: Dados que precisa ler depois
/// 
/// HASHING:
/// - Irreversível (não pode "des-hashear")
/// - One-way function
/// - Exemplo: BCrypt, Argon2, PBKDF2
/// - Uso: Senhas!
/// 
/// POR QUE BCRYPT?
/// ✅ Automaticamente adiciona salt (previne rainbow tables)
/// ✅ Adaptativo (pode aumentar custo computacional)
/// ✅ Lento de propósito (dificulta brute force)
/// ✅ Battle-tested há décadas
/// 
/// ALTERNATIVAS:
/// - Argon2: Mais moderno, vencedor da Password Hashing Competition
/// - PBKDF2: Padrão NIST
/// - Scrypt: Resistente a hardware especializado
/// 
/// ❌ NUNCA USE:
/// - MD5: Quebrado, rápido demais
/// - SHA1: Quebrado
/// - SHA256 sem salt: Vulnerável a rainbow tables
/// 
/// COMO FUNCIONA A VALIDAÇÃO:
/// 1. Usuário envia senha
/// 2. BCrypt pega o hash armazenado
/// 3. Extrai o salt do hash
/// 4. Hashea a senha com o mesmo salt
/// 5. Compara os hashes
/// 
/// WORK FACTOR:
/// O parâmetro workFactor controla quantas iterações:
/// - workFactor 10 = 2^10 = 1024 iterações (~100ms)
/// - workFactor 12 = 2^12 = 4096 iterações (~400ms)
/// - workFactor 14 = 2^14 = 16384 iterações (~1.6s)
/// 
/// Recomendado: 12 para maioria dos casos
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Gera hash da senha
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifica se senha corresponde ao hash
    /// </summary>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica se o hash precisa ser atualizado (work factor mudou)
    /// Útil para melhorar segurança gradualmente
    /// </summary>
    public bool NeedsUpgrade(string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.PasswordNeedsRehash(hashedPassword, WorkFactor);
        }
        catch
        {
            return true;
        }
    }
}
