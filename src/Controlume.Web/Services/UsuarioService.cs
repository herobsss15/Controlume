using Controlume.Web.Data;
using Controlume.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public class UsuarioService(ControlumeDbContext db, IPasswordHasher<Usuario> passwordHasher)
{
    /// <summary>Login é sempre comparado e gravado em minúsculas, para o índice único valer de verdade.</summary>
    public static string NormalizarLogin(string login) => login.Trim().ToLowerInvariant();

    /// <summary>Retorna o usuário quando login e senha conferem e a conta está ativa; <c>null</c> em qualquer outro caso.</summary>
    public async Task<Usuario?> ValidarCredenciaisAsync(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(senha))
        {
            return null;
        }

        var normalizado = NormalizarLogin(login);
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Login == normalizado && u.Ativo);
        if (usuario is null)
        {
            return null;
        }

        var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
        return resultado == PasswordVerificationResult.Failed ? null : usuario;
    }
}
