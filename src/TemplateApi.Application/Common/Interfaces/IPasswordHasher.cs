namespace TemplateApi.Application.Common.Interfaces;

/// <summary>
/// Serviço de hashing de senhas
/// Interface para não violar Clean Architecture (Application não pode depender de Infrastructure)
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera hash de uma senha usando BCrypt
    /// </summary>
    /// <param name="password">Senha em texto plano</param>
    /// <returns>Hash da senha</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifica se uma senha corresponde ao hash
    /// </summary>
    /// <param name="password">Senha em texto plano</param>
    /// <param name="passwordHash">Hash armazenado</param>
    /// <returns>True se a senha está correta</returns>
    bool VerifyPassword(string password, string passwordHash);
}
