using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

public class VendaServiceTests
{
    [Fact]
    public async Task ConfirmarVendaAsync_LancaExcecao_QuandoNaoHaCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        await Assert.ThrowsAsync<NenhumCaixaAbertoException>(() =>
            vendaService.ConfirmarVendaAsync(
                [new ItemVendaEntrada(produto.Id, 1, 10m)],
                [new PagamentoEntrada(Formas.Dinheiro, 10m)]));
    }

    [Fact]
    public async Task ConfirmarVendaAsync_LancaExcecao_QuandoSomaDePagamentosNaoBateComTotal()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        await Assert.ThrowsAsync<PagamentoDivergenteException>(() =>
            vendaService.ConfirmarVendaAsync(
                [new ItemVendaEntrada(produto.Id, 2, 10m)], // total = 20
                [new PagamentoEntrada(Formas.Dinheiro, 15m)])); // só 15
    }

    [Fact]
    public async Task ConfirmarVendaAsync_AceitaPagamentoDivididoQuandoSomaBate()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 2, 10m)], // total = 20
            [new PagamentoEntrada(Formas.Cartao, 12m), new PagamentoEntrada(Formas.Dinheiro, 8m)]);

        Assert.Equal(20m, venda.ValorTotal);
    }

    [Fact]
    public async Task ConfirmarVendaAsync_LancaExcecao_QuandoEstoqueInsuficiente()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 1);

        await Assert.ThrowsAsync<EstoqueInsuficienteException>(() =>
            vendaService.ConfirmarVendaAsync(
                [new ItemVendaEntrada(produto.Id, 2, 10m)], // pede 2, só tem 1
                [new PagamentoEntrada(Formas.Dinheiro, 20m)]));
    }

    [Fact]
    public async Task ConfirmarVendaAsync_NaoAlteraEstoque_QuandoVendaFalha()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var produtoService = db.CriarProdutoService();
        await caixaService.AbrirCaixaAsync(0m);
        var produtoSeed = await db.SeedProdutoAsync(preco: 10m, estoque: 1);

        await Assert.ThrowsAsync<EstoqueInsuficienteException>(() =>
            vendaService.ConfirmarVendaAsync(
                [new ItemVendaEntrada(produtoSeed.Id, 2, 10m)],
                [new PagamentoEntrada(Formas.Dinheiro, 20m)]));

        var produto = await produtoService.ObterPorIdAsync(produtoSeed.Id);
        Assert.Equal(1, produto!.QuantidadeEstoque);
    }

    [Fact]
    public async Task ConfirmarVendaAsync_DecrementaEstoqueAposVendaValida()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var produtoService = db.CriarProdutoService();
        await caixaService.AbrirCaixaAsync(0m);
        var produtoSeed = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produtoSeed.Id, 3, 10m)],
            [new PagamentoEntrada(Formas.Dinheiro, 30m)]);

        var produto = await produtoService.ObterPorIdAsync(produtoSeed.Id);
        Assert.Equal(2, produto!.QuantidadeEstoque);
    }

    [Fact]
    public async Task ConfirmarVendaAsync_MantemPrecoTabelaCongelado_MesmoAposMudarPrecoDoProduto()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var produtoService = db.CriarProdutoService();
        await caixaService.AbrirCaixaAsync(0m);
        var produtoSeed = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        // Vende com desconto: PrecoVenda (8) diverge do preço de tabela no momento (10).
        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produtoSeed.Id, 1, 8m)],
            [new PagamentoEntrada(Formas.Dinheiro, 8m)]);

        // Preço do produto muda depois da venda já confirmada.
        await produtoService.AtualizarAsync(new Produto
        {
            Id = produtoSeed.Id,
            Nome = produtoSeed.Nome,
            TipoProdutoId = produtoSeed.TipoProdutoId,
            Preco = 99m,
            QuantidadeEstoque = 4
        });

        var detalhe = await vendaService.ObterDetalheAsync(venda.Id);
        var item = detalhe!.Itens.Single();

        Assert.Equal(10m, item.PrecoTabela); // regra 4: congelado, não virou 99
        Assert.Equal(8m, item.PrecoVenda);   // regra 5: divergência preservada
    }
}
