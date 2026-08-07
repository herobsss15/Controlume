using Controlume.Web.Domain;
using Controlume.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Data;

/// <summary>Um usuário da seção de configuração <c>Usuarios:Seed</c>.</summary>
public class UsuarioSeed
{
    public string? Nome { get; set; }
    public string? Login { get; set; }
    public string? Senha { get; set; }
    public Role Role { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>
/// Não há tela de cadastro de usuário: os poucos usuários fixos vêm da configuração
/// (variáveis de ambiente em produção) e são sincronizados no startup. Trocar a senha é
/// trocar a variável e reiniciar — o hash é regravado quando deixa de bater com a config.
/// </summary>
public static class UsuarioSeeder
{
    public static async Task SincronizarAsync(
        ControlumeDbContext db,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var configurados = configuration.GetSection("Usuarios:Seed").Get<List<UsuarioSeed>>() ?? [];

        foreach (var seed in configurados)
        {
            if (string.IsNullOrWhiteSpace(seed.Login) || string.IsNullOrEmpty(seed.Senha))
            {
                logger.LogWarning(
                    "Usuário de seed \"{Login}\" ignorado: Login e Senha precisam estar configurados.",
                    seed.Login ?? "(sem login)");
                continue;
            }

            var login = UsuarioService.NormalizarLogin(seed.Login);
            var nome = string.IsNullOrWhiteSpace(seed.Nome) ? seed.Login.Trim() : seed.Nome.Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Login == login);

            if (usuario is null)
            {
                usuario = new Usuario
                {
                    Nome = nome,
                    Login = login,
                    SenhaHash = "",
                    Role = seed.Role,
                    Ativo = seed.Ativo
                };
                db.Usuarios.Add(usuario);
                logger.LogInformation("Usuário \"{Login}\" ({Role}) criado a partir da configuração.", login, seed.Role);
            }
            else
            {
                usuario.Nome = nome;
                usuario.Role = seed.Role;
                usuario.Ativo = seed.Ativo;
            }

            // Rehash só quando a senha configurada não confere: evita gravar um hash novo
            // (salt aleatório) a cada startup só porque o hash nunca é igual ao anterior.
            var senhaConfere = usuario.SenhaHash.Length > 0
                && passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, seed.Senha) != PasswordVerificationResult.Failed;
            if (!senhaConfere)
            {
                usuario.SenhaHash = passwordHasher.HashPassword(usuario, seed.Senha);
                logger.LogInformation("Senha do usuário \"{Login}\" atualizada a partir da configuração.", login);
            }
        }

        await db.SaveChangesAsync();

        if (!await db.Usuarios.AnyAsync(u => u.Ativo && u.Role == Role.Admin))
        {
            logger.LogWarning(
                "Nenhum usuário Admin ativo cadastrado — ninguém consegue entrar no sistema. "
                + "Configure Usuarios:Seed (ex.: Usuarios__Seed__0__Login / __Senha / __Role).");
        }
    }
}
