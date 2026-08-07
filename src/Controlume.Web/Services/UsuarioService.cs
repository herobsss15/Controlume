using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

/// <summary>
/// Gestão de usuários é exclusiva do Admin — inclusive a leitura da lista, que expõe quem
/// tem acesso ao sistema. É a única tela em que o Stakeholder não entra nem para consultar.
/// </summary>
public class UsuarioService(ControlumeDbContext db, IPasswordHasher<Usuario> passwordHasher, IUsuarioAtual usuarioAtual)
{
    public const int TamanhoMinimoSenha = 8;

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

    public async Task<List<Usuario>> ListarAsync()
    {
        await usuarioAtual.GarantirAdminAsync();

        return await db.Usuarios.AsNoTracking()
            .OrderBy(u => u.Nome)
            .ToListAsync();
    }

    public async Task<Usuario> CriarAsync(string nome, string login, string senha, Role role)
    {
        await usuarioAtual.GarantirAdminAsync();

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(login))
        {
            throw new DadosDoUsuarioIncompletosException();
        }

        GarantirSenhaForte(senha);

        var normalizado = NormalizarLogin(login);
        if (await db.Usuarios.AnyAsync(u => u.Login == normalizado))
        {
            throw new LoginJaExisteException(normalizado);
        }

        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Login = normalizado,
            SenhaHash = "",
            Role = role,
            Ativo = true
        };
        usuario.SenhaHash = passwordHasher.HashPassword(usuario, senha);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    /// <summary>Nome e papel são editáveis; o login é a identidade do usuário e não muda.</summary>
    public async Task AtualizarAsync(int id, string nome, Role role)
    {
        await usuarioAtual.GarantirAdminAsync();

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DadosDoUsuarioIncompletosException();
        }

        var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);

        if (role != Role.Admin && await EhUltimoAdminAtivoAsync(usuario))
        {
            throw new UltimoAdminException();
        }

        usuario.Nome = nome.Trim();
        usuario.Role = role;
        await db.SaveChangesAsync();
    }

    /// <summary>Reset de senha é sempre definido pelo Admin: não há fluxo de recuperação por e-mail.</summary>
    public async Task RedefinirSenhaAsync(int id, string novaSenha)
    {
        await usuarioAtual.GarantirAdminAsync();
        GarantirSenhaForte(novaSenha);

        var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);
        usuario.SenhaHash = passwordHasher.HashPassword(usuario, novaSenha);
        await db.SaveChangesAsync();
    }

    public async Task DesativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        if (await usuarioAtual.ObterIdAsync() == id)
        {
            throw new NaoPodeDesativarPropriaContaException();
        }

        var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);
        if (await EhUltimoAdminAtivoAsync(usuario))
        {
            throw new UltimoAdminException();
        }

        usuario.Ativo = false;
        await db.SaveChangesAsync();
    }

    public async Task ReativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);
        usuario.Ativo = true;
        await db.SaveChangesAsync();
    }

    private static void GarantirSenhaForte(string senha)
    {
        if (senha is null || senha.Length < TamanhoMinimoSenha)
        {
            throw new SenhaCurtaException(TamanhoMinimoSenha);
        }
    }

    private async Task<bool> EhUltimoAdminAtivoAsync(Usuario usuario)
        => usuario is { Role: Role.Admin, Ativo: true }
            && !await db.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.Ativo && u.Role == Role.Admin);
}
