using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

public class CaixaServiceTests
{
    [Fact]
    public async Task AbrirCaixaAsync_CriaCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var service = new CaixaService(db.Context);

        var caixa = await service.AbrirCaixaAsync(100m);

        Assert.Equal(StatusCaixa.Aberto, caixa.Status);
        Assert.Equal(100m, caixa.ValorInicial);
        Assert.Null(caixa.DataFechamento);
    }

    [Fact]
    public async Task AbrirCaixaAsync_LancaExcecao_QuandoJaExisteCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var service = new CaixaService(db.Context);
        await service.AbrirCaixaAsync(100m);

        await Assert.ThrowsAsync<CaixaJaAbertoException>(() => service.AbrirCaixaAsync(50m));
    }

    [Fact]
    public async Task FecharCaixaAsync_PermiteAbrirNovoCaixaDepois()
    {
        using var db = new TestDbContextFactory();
        var service = new CaixaService(db.Context);
        var caixa = await service.AbrirCaixaAsync(100m);

        await service.FecharCaixaAsync(caixa.Id);
        var novoCaixa = await service.AbrirCaixaAsync(50m);

        Assert.NotEqual(caixa.Id, novoCaixa.Id);
        Assert.Equal(StatusCaixa.Aberto, novoCaixa.Status);
    }

    [Fact]
    public async Task ObterResumoAsync_CalculaTotalPorFormaDePagamento()
    {
        using var db = new TestDbContextFactory();
        var caixaService = new CaixaService(db.Context);
        var vendaService = new VendaService(db.Context);

        var caixa = await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);

        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 2, 50m)],
            [new PagamentoEntrada(TipoPagamento.Dinheiro, 60m), new PagamentoEntrada(TipoPagamento.Pix, 40m)]);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);

        Assert.Equal(1, resumo.QuantidadeVendas);
        Assert.Equal(100m, resumo.TotalVendas);
        Assert.Equal(60m, resumo.PorFormaPagamento.Single(f => f.TipoPagamento == TipoPagamento.Dinheiro).Total);
        Assert.Equal(40m, resumo.PorFormaPagamento.Single(f => f.TipoPagamento == TipoPagamento.Pix).Total);
    }
}
