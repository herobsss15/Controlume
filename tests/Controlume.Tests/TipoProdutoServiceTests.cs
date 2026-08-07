using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

public class TipoProdutoServiceTests
{
    [Fact]
    public async Task ExcluirAsync_LancaExcecao_QuandoHaProdutoReferenciando()
    {
        using var db = new TestDbContextFactory();
        var tipoService = db.CriarTipoProdutoService();
        var produtoService = db.CriarProdutoService();

        var tipo = await tipoService.CriarAsync("Fitas K7");
        await produtoService.CriarAsync(new Produto { Nome = "Fita X", TipoProdutoId = tipo.Id, Preco = 5m, QuantidadeEstoque = 1 });

        await Assert.ThrowsAsync<TipoProdutoReferenciadoException>(() => tipoService.ExcluirAsync(tipo.Id));
    }

    [Fact]
    public async Task ExcluirAsync_LancaExcecao_MesmoQuandoProdutoReferenciadorEstaInativo()
    {
        using var db = new TestDbContextFactory();
        var tipoService = db.CriarTipoProdutoService();
        var produtoService = db.CriarProdutoService();

        var tipo = await tipoService.CriarAsync("Fitas K7");
        var produto = await produtoService.CriarAsync(new Produto { Nome = "Fita X", TipoProdutoId = tipo.Id, Preco = 5m, QuantidadeEstoque = 1 });
        await produtoService.DesativarAsync(produto.Id);

        await Assert.ThrowsAsync<TipoProdutoReferenciadoException>(() => tipoService.ExcluirAsync(tipo.Id));
    }

    [Fact]
    public async Task ExcluirAsync_RemoveTipo_QuandoNaoHaProdutoReferenciando()
    {
        using var db = new TestDbContextFactory();
        var tipoService = db.CriarTipoProdutoService();

        var tipo = await tipoService.CriarAsync("Categoria sem uso");
        await tipoService.ExcluirAsync(tipo.Id);

        var encontrado = await tipoService.ObterPorIdAsync(tipo.Id);
        Assert.Null(encontrado);
    }

    [Fact]
    public async Task DesativarAsync_NaoRemoveOTipo_ApenasMarcaComoInativo()
    {
        using var db = new TestDbContextFactory();
        var tipoService = db.CriarTipoProdutoService();
        var tipo = await tipoService.CriarAsync("Categoria X");

        await tipoService.DesativarAsync(tipo.Id);

        var listaAtivos = await tipoService.ListarAsync(incluirInativos: false);
        var listaTodos = await tipoService.ListarAsync(incluirInativos: true);

        Assert.DoesNotContain(listaAtivos, t => t.Id == tipo.Id);
        Assert.Contains(listaTodos, t => t.Id == tipo.Id);
    }

    [Fact]
    public async Task ListarAsync_IncluiOsCincoTiposSeedados()
    {
        using var db = new TestDbContextFactory();
        var tipoService = db.CriarTipoProdutoService();

        var tipos = await tipoService.ListarAsync();

        Assert.Contains(tipos, t => t.Nome == "Disco");
        Assert.Contains(tipos, t => t.Nome == "CD");
        Assert.Contains(tipos, t => t.Nome == "DVD");
        Assert.Contains(tipos, t => t.Nome == "Eletrônico");
        Assert.Contains(tipos, t => t.Nome == "Avulso");
    }
}
