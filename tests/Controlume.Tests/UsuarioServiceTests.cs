using Controlume.Web.Data;
using Controlume.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Controlume.Tests;

public class UsuarioServiceTests
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    private static IConfiguration Config(params (string Login, string Senha, string Role)[] usuarios)
    {
        var valores = new Dictionary<string, string?>();
        for (var i = 0; i < usuarios.Length; i++)
        {
            valores[$"Usuarios:Seed:{i}:Nome"] = usuarios[i].Login;
            valores[$"Usuarios:Seed:{i}:Login"] = usuarios[i].Login;
            valores[$"Usuarios:Seed:{i}:Senha"] = usuarios[i].Senha;
            valores[$"Usuarios:Seed:{i}:Role"] = usuarios[i].Role;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
    }

    private static Task SincronizarAsync(TestDbContextFactory db, IConfiguration configuration)
        => UsuarioSeeder.SincronizarAsync(db.Context, Hasher, configuration, NullLogger.Instance);

    [Fact]
    public async Task ValidarCredenciaisAsync_RetornaUsuario_QuandoSenhaConfere()
    {
        using var db = new TestDbContextFactory();
        await SincronizarAsync(db, Config(("gui", "senha-forte", nameof(Role.Admin))));

        var usuario = await db.CriarUsuarioService().ValidarCredenciaisAsync("gui", "senha-forte");

        Assert.NotNull(usuario);
        Assert.Equal(Role.Admin, usuario.Role);
    }

    [Fact]
    public async Task ValidarCredenciaisAsync_RetornaNull_QuandoSenhaEstaErrada()
    {
        using var db = new TestDbContextFactory();
        await SincronizarAsync(db, Config(("gui", "senha-forte", nameof(Role.Admin))));

        Assert.Null(await db.CriarUsuarioService().ValidarCredenciaisAsync("gui", "senha-fraca"));
    }

    [Fact]
    public async Task ValidarCredenciaisAsync_RetornaNull_QuandoLoginNaoExiste()
    {
        using var db = new TestDbContextFactory();

        Assert.Null(await db.CriarUsuarioService().ValidarCredenciaisAsync("ninguem", "qualquer"));
    }

    [Fact]
    public async Task ValidarCredenciaisAsync_IgnoraCaixaAltaEEspacosNoLogin()
    {
        using var db = new TestDbContextFactory();
        await SincronizarAsync(db, Config(("gui", "senha-forte", nameof(Role.Admin))));

        Assert.NotNull(await db.CriarUsuarioService().ValidarCredenciaisAsync("  GUI ", "senha-forte"));
    }

    [Fact]
    public async Task ValidarCredenciaisAsync_RetornaNull_QuandoUsuarioEstaInativo()
    {
        using var db = new TestDbContextFactory();
        await SincronizarAsync(db, Config(("gui", "senha-forte", nameof(Role.Admin))));

        var usuario = db.Context.Usuarios.Single();
        usuario.Ativo = false;
        await db.Context.SaveChangesAsync();

        Assert.Null(await db.CriarUsuarioService().ValidarCredenciaisAsync("gui", "senha-forte"));
    }

    [Fact]
    public async Task Seeder_NaoDuplicaUsuarioNemRegravaHash_QuandoRodaDeNovo()
    {
        using var db = new TestDbContextFactory();
        var configuracao = Config(("gui", "senha-forte", nameof(Role.Admin)));
        await SincronizarAsync(db, configuracao);
        var hashInicial = db.Context.Usuarios.Single().SenhaHash;

        await SincronizarAsync(db, configuracao);

        var usuario = Assert.Single(db.Context.Usuarios);
        Assert.Equal(hashInicial, usuario.SenhaHash);
    }

    [Fact]
    public async Task Seeder_TrocaSenha_QuandoAConfiguracaoMuda()
    {
        using var db = new TestDbContextFactory();
        await SincronizarAsync(db, Config(("gui", "senha-antiga", nameof(Role.Admin))));

        await SincronizarAsync(db, Config(("gui", "senha-nova", nameof(Role.Admin))));

        var service = db.CriarUsuarioService();
        Assert.Null(await service.ValidarCredenciaisAsync("gui", "senha-antiga"));
        Assert.NotNull(await service.ValidarCredenciaisAsync("gui", "senha-nova"));
    }

    [Fact]
    public async Task Seeder_CriaOsTresPapeis()
    {
        using var db = new TestDbContextFactory();

        await SincronizarAsync(db, Config(
            ("gui", "s1", nameof(Role.Admin)),
            ("loja", "s2", nameof(Role.Operador)),
            ("socio", "s3", nameof(Role.Stakeholder))));

        var service = db.CriarUsuarioService();
        Assert.Equal(Role.Admin, (await service.ValidarCredenciaisAsync("gui", "s1"))!.Role);
        Assert.Equal(Role.Operador, (await service.ValidarCredenciaisAsync("loja", "s2"))!.Role);
        Assert.Equal(Role.Stakeholder, (await service.ValidarCredenciaisAsync("socio", "s3"))!.Role);
    }
}
