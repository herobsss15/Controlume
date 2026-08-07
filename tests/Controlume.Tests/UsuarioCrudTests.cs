using System.Security.Claims;
using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

/// <summary>Tela de usuários: só Admin cria, edita e desativa — e ninguém apaga o último Admin.</summary>
public class UsuarioCrudTests
{
    private const string SenhaValida = "senha-de-teste";

    [Fact]
    public async Task CriarAsync_NormalizaLoginEPermiteEntrarComANovaSenha()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();

        var usuario = await service.CriarAsync("Operador da Loja", "  LOJA ", SenhaValida, Role.Operador);

        Assert.Equal("loja", usuario.Login);
        Assert.Equal("Operador da Loja", usuario.Nome);
        Assert.Equal(Role.Operador, usuario.Role);
        Assert.True(usuario.Ativo);
        Assert.NotEqual(SenhaValida, usuario.SenhaHash);
        Assert.NotNull(await service.ValidarCredenciaisAsync("loja", SenhaValida));
    }

    [Fact]
    public async Task CriarAsync_LancaExcecao_QuandoLoginJaExisteMesmoComOutraCaixa()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        await service.CriarAsync("Operador", "loja", SenhaValida, Role.Operador);

        await Assert.ThrowsAsync<LoginJaExisteException>(() =>
            service.CriarAsync("Outro", "LOJA", SenhaValida, Role.Operador));
    }

    [Fact]
    public async Task CriarAsync_LancaExcecao_QuandoSenhaEhCurta()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();

        await Assert.ThrowsAsync<SenhaCurtaException>(() =>
            service.CriarAsync("Operador", "loja", new string('a', UsuarioService.TamanhoMinimoSenha - 1), Role.Operador));
    }

    [Theory]
    [InlineData("", "loja")]
    [InlineData("Operador", "   ")]
    public async Task CriarAsync_LancaExcecao_QuandoNomeOuLoginEstaVazio(string nome, string login)
    {
        using var db = new TestDbContextFactory();

        await Assert.ThrowsAsync<DadosDoUsuarioIncompletosException>(() =>
            db.CriarUsuarioService().CriarAsync(nome, login, SenhaValida, Role.Operador));
    }

    [Fact]
    public async Task AtualizarAsync_TrocaNomeEPapel()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", SenhaValida, Role.Operador);

        await service.AtualizarAsync(usuario.Id, "Operador Renomeado", Role.Stakeholder);

        var atualizado = (await service.ListarAsync()).Single(u => u.Id == usuario.Id);
        Assert.Equal("Operador Renomeado", atualizado.Nome);
        Assert.Equal(Role.Stakeholder, atualizado.Role);
    }

    [Fact]
    public async Task AtualizarAsync_NaoRebaixaOUltimoAdminAtivo()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var admin = await service.CriarAsync("Admin", "gui", SenhaValida, Role.Admin);

        await Assert.ThrowsAsync<UltimoAdminException>(() =>
            service.AtualizarAsync(admin.Id, "Admin", Role.Operador));
    }

    [Fact]
    public async Task AtualizarAsync_RebaixaAdmin_QuandoExisteOutroAdminAtivo()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var primeiro = await service.CriarAsync("Admin 1", "gui", SenhaValida, Role.Admin);
        await service.CriarAsync("Admin 2", "outro", SenhaValida, Role.Admin);

        await service.AtualizarAsync(primeiro.Id, "Admin 1", Role.Operador);

        Assert.Equal(Role.Operador, (await service.ListarAsync()).Single(u => u.Id == primeiro.Id).Role);
    }

    [Fact]
    public async Task DesativarAsync_NaoDesativaOUltimoAdminAtivo()
    {
        using var db = new TestDbContextFactory();
        var admin = await db.CriarUsuarioService().CriarAsync("Admin", "gui", SenhaValida, Role.Admin);
        // Outro admin é quem executa a ação, para não esbarrar antes na regra da própria conta.
        var service = db.CriarUsuarioService(Role.Admin, id: admin.Id + 999);

        await Assert.ThrowsAsync<UltimoAdminException>(() => service.DesativarAsync(admin.Id));
    }

    [Fact]
    public async Task DesativarAsync_NaoDesativaAPropriaConta()
    {
        using var db = new TestDbContextFactory();
        var criador = db.CriarUsuarioService();
        var admin = await criador.CriarAsync("Admin", "gui", SenhaValida, Role.Admin);
        await criador.CriarAsync("Admin 2", "outro", SenhaValida, Role.Admin);

        var comoEleMesmo = db.CriarUsuarioService(Role.Admin, id: admin.Id);

        await Assert.ThrowsAsync<NaoPodeDesativarPropriaContaException>(() => comoEleMesmo.DesativarAsync(admin.Id));
    }

    [Fact]
    public async Task DesativarAsync_ImpedeLoginEReativarLiberaDeNovo()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", SenhaValida, Role.Operador);

        await service.DesativarAsync(usuario.Id);
        Assert.Null(await service.ValidarCredenciaisAsync("loja", SenhaValida));

        await service.ReativarAsync(usuario.Id);
        Assert.NotNull(await service.ValidarCredenciaisAsync("loja", SenhaValida));
    }

    [Fact]
    public async Task RedefinirSenhaAsync_InvalidaASenhaAntiga()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", SenhaValida, Role.Operador);

        await service.RedefinirSenhaAsync(usuario.Id, "outra-senha-boa");

        Assert.Null(await service.ValidarCredenciaisAsync("loja", SenhaValida));
        Assert.NotNull(await service.ValidarCredenciaisAsync("loja", "outra-senha-boa"));
    }

    [Fact]
    public async Task RedefinirSenhaAsync_LancaExcecao_QuandoSenhaEhCurta()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", SenhaValida, Role.Operador);

        await Assert.ThrowsAsync<SenhaCurtaException>(() => service.RedefinirSenhaAsync(usuario.Id, "curta"));
    }

    /// <summary>Nem o Stakeholder consulta esta tela: a lista mostra quem tem acesso ao sistema.</summary>
    [Theory]
    [InlineData(Role.Operador)]
    [InlineData(Role.Stakeholder)]
    [InlineData(null)]
    public async Task GestaoDeUsuarios_EhExclusivaDoAdmin(Role? role)
    {
        using var db = new TestDbContextFactory();
        var usuario = await db.CriarUsuarioService().CriarAsync("Operador", "loja", SenhaValida, Role.Operador);
        var service = db.CriarUsuarioService(role);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.ListarAsync());
        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.CriarAsync("X", "x", SenhaValida, Role.Admin));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.AtualizarAsync(usuario.Id, "X", Role.Admin));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.RedefinirSenhaAsync(usuario.Id, SenhaValida));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.DesativarAsync(usuario.Id));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => service.ReativarAsync(usuario.Id));
    }
}

/// <summary>
/// O cookie carrega papel e id do momento do login; a revalidação é o que faz desativar ou
/// rebaixar alguém ter efeito antes de o cookie expirar.
/// </summary>
public class RevalidacaoDeSessaoTests
{
    private static ClaimsPrincipal Principal(int id, Role role)
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role.ToString())],
            "Cookies"));

    [Fact]
    public async Task ContinuaValida_QuandoUsuarioSegueAtivoComOMesmoPapel()
    {
        using var db = new TestDbContextFactory();
        var usuario = await db.CriarUsuarioService().CriarAsync("Operador", "loja", "senha-de-teste", Role.Operador);

        Assert.True(await RevalidacaoDeSessao.ContinuaValidaAsync(db.Context, Principal(usuario.Id, Role.Operador)));
    }

    [Fact]
    public async Task NaoContinuaValida_QuandoUsuarioFoiDesativado()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", "senha-de-teste", Role.Operador);
        await service.DesativarAsync(usuario.Id);

        Assert.False(await RevalidacaoDeSessao.ContinuaValidaAsync(db.Context, Principal(usuario.Id, Role.Operador)));
    }

    [Fact]
    public async Task NaoContinuaValida_QuandoOPapelMudouDepoisDoLogin()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarUsuarioService();
        var usuario = await service.CriarAsync("Operador", "loja", "senha-de-teste", Role.Operador);
        await service.AtualizarAsync(usuario.Id, "Operador", Role.Stakeholder);

        Assert.False(await RevalidacaoDeSessao.ContinuaValidaAsync(db.Context, Principal(usuario.Id, Role.Operador)));
    }

    [Fact]
    public async Task NaoContinuaValida_QuandoOCookieNaoTemIdDeUsuario()
    {
        using var db = new TestDbContextFactory();

        Assert.False(await RevalidacaoDeSessao.ContinuaValidaAsync(db.Context, new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
