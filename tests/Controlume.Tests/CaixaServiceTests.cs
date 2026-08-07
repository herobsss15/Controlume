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
        var service = db.CriarCaixaService();

        var caixa = await service.AbrirCaixaAsync(100m);

        Assert.Equal(StatusCaixa.Aberto, caixa.Status);
        Assert.Equal(100m, caixa.ValorInicial);
        Assert.Null(caixa.DataFechamento);
    }

    [Fact]
    public async Task AbrirCaixaAsync_LancaExcecao_QuandoJaExisteCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarCaixaService();
        await service.AbrirCaixaAsync(100m);

        await Assert.ThrowsAsync<CaixaJaAbertoException>(() => service.AbrirCaixaAsync(50m));
    }

    [Fact]
    public async Task FecharCaixaAsync_PermiteAbrirNovoCaixaDepois()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarCaixaService();
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
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();

        var caixa = await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);

        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 2, 50m)],
            [new PagamentoEntrada(Formas.Dinheiro, 60m), new PagamentoEntrada(Formas.Pix, 40m)]);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);

        Assert.Equal(1, resumo.QuantidadeVendas);
        Assert.Equal(100m, resumo.TotalVendas);
        Assert.Equal(60m, resumo.PorFormaPagamento.Single(f => f.FormaPagamentoId == Formas.Dinheiro).Total);
        Assert.Equal(40m, resumo.PorFormaPagamento.Single(f => f.FormaPagamentoId == Formas.Pix).Total);
    }

    /// <summary>Regra 14: só o dinheiro entra no saldo da gaveta, e as sangrias saem dele.</summary>
    [Fact]
    public async Task ObterResumoAsync_CalculaSaldoEmDinheiroDescontandoSangrias()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var sangriaService = db.CriarSangriaService();

        var caixa = await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);
        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 2, 50m)],
            [new PagamentoEntrada(Formas.Dinheiro, 60m), new PagamentoEntrada(Formas.Cartao, 40m)]);
        await sangriaService.RegistrarAsync(MotivoSangria.Compra, 30m, null);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);

        Assert.Equal(30m, resumo.TotalSangrias);
        Assert.Equal(130m, resumo.SaldoEmDinheiro); // 100 inicial + 60 dinheiro − 30 sangria
        Assert.Equal(100m, resumo.TotalVendas);     // os 40 do cartão contam na venda, não na gaveta
        Assert.Null(resumo.SaldoFinal);             // ainda aberto
    }

    [Fact]
    public async Task FecharCaixaAsync_GravaSaldoFinalConsiderandoSangrias()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var sangriaService = db.CriarSangriaService();

        var caixa = await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);
        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 50m)],
            [new PagamentoEntrada(Formas.Dinheiro, 50m)]);
        await sangriaService.RegistrarAsync(MotivoSangria.Pagamento, 20m, "fornecedor");

        await caixaService.FecharCaixaAsync(caixa.Id);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);
        Assert.Equal(130m, resumo.SaldoFinal); // 100 + 50 − 20
    }

    /// <summary>Regra 15: o SaldoFinal do último fechamento vira a sugestão da próxima abertura.</summary>
    [Fact]
    public async Task ObterSugestaoValorInicialAsync_UsaSaldoFinalDoUltimoCaixaFechado()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var sangriaService = db.CriarSangriaService();

        var caixa = await caixaService.AbrirCaixaAsync(300m);
        await sangriaService.RegistrarAsync(MotivoSangria.Compra, 45m, null);
        await caixaService.FecharCaixaAsync(caixa.Id);

        Assert.Equal(255m, await caixaService.ObterSugestaoValorInicialAsync());
    }

    [Fact]
    public async Task ObterSugestaoValorInicialAsync_RetornaZero_QuandoNaoHaFechamentoAnterior()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();

        Assert.Equal(0m, await caixaService.ObterSugestaoValorInicialAsync());
    }

    [Fact]
    public async Task ObterSugestaoValorInicialAsync_IgnoraCaixaAindaAberto()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();

        var primeiro = await caixaService.AbrirCaixaAsync(80m);
        await caixaService.FecharCaixaAsync(primeiro.Id);
        await caixaService.AbrirCaixaAsync(500m);

        Assert.Equal(80m, await caixaService.ObterSugestaoValorInicialAsync());
    }
}
