using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

/// <summary>Regras 21 e 22: de onde vem o Recebido e por que ele não volta atrás.</summary>
public class RecebimentoPagamentoTests
{
    [Theory]
    [InlineData(Formas.Dinheiro)]
    [InlineData(Formas.Cartao)]
    [InlineData(Formas.Pix)]
    public async Task Pagamento_NasceRecebido_QuandoAFormaNaoExigeConfirmacao(int formaId)
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(formaId, 10m)]);

        var pagamento = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single();
        Assert.True(pagamento.Recebido);
        Assert.NotNull(pagamento.DataRecebimento);
    }

    [Fact]
    public async Task Pagamento_NasceAguardando_QuandoAFormaExigeConfirmacao()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(Formas.MercadoLivre, 10m)]);

        var pagamento = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single();
        Assert.False(pagamento.Recebido);
        Assert.Null(pagamento.DataRecebimento);
    }

    [Fact]
    public async Task MarcarComoRecebido_ConfirmaOPagamentoPendenteEGravaAData()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(Formas.MercadoLivre, 10m)]);
        var pagamentoId = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single().Id;

        await vendaService.MarcarPagamentoComoRecebidoAsync(pagamentoId);

        var pagamento = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single();
        Assert.True(pagamento.Recebido);
        Assert.NotNull(pagamento.DataRecebimento);
    }

    /// <summary>
    /// Regra 22: o service não expõe nenhum caminho de volta, e remarcar o que já está recebido
    /// é recusado — inclusive nas formas que já nascem recebidas.
    /// </summary>
    [Theory]
    [InlineData(Formas.Dinheiro)]
    [InlineData(Formas.MercadoLivre)]
    public async Task MarcarComoRecebido_EhRecusado_QuandoOPagamentoJaEstaRecebido(int formaId)
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(formaId, 10m)]);
        var pagamentoId = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single().Id;

        if (formaId == Formas.MercadoLivre)
        {
            await vendaService.MarcarPagamentoComoRecebidoAsync(pagamentoId);
        }

        await Assert.ThrowsAsync<PagamentoJaRecebidoException>(
            () => vendaService.MarcarPagamentoComoRecebidoAsync(pagamentoId));

        var pagamento = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single();
        Assert.True(pagamento.Recebido);
    }

    [Fact]
    public async Task Stakeholder_NaoMarcaPagamentoComoRecebido()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var vendaService = db.CriarVendaService();
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(Formas.MercadoLivre, 10m)]);
        var pagamentoId = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single().Id;

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            db.CriarVendaService(Role.Stakeholder).MarcarPagamentoComoRecebidoAsync(pagamentoId));
    }

    /// <summary>
    /// Regra 23: uma venda por um canal sem caixa físico não engorda a gaveta — e não é barrada
    /// por isso, já que nenhuma validação de saldo se aplica a ela.
    /// </summary>
    [Fact]
    public async Task VendaPorCanalExterno_NaoEntraNoSaldoEmDinheiro_MasContaNoTotalVendido()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var caixa = await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);

        await db.CriarVendaService().ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 50m)],
            [new PagamentoEntrada(Formas.MercadoLivre, 50m)]);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);
        Assert.Equal(50m, resumo.TotalVendas);
        Assert.Equal(100m, resumo.SaldoEmDinheiro); // segue sendo só o valor inicial
        Assert.False(resumo.PorFormaPagamento.Single().ContaComoCaixaFisico);
    }

    /// <summary>
    /// Regra 23 pela flag, não pelo nome: ligar ContaComoCaixaFisico em outra forma passa a
    /// contá-la na gaveta sem nenhuma mudança de código.
    /// </summary>
    [Fact]
    public async Task SaldoEmDinheiro_SegueAFlagDaForma_NaoONomeDela()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        await db.CriarFormaPagamentoService().AtualizarAsync(
            Formas.Pix, "Pix", contaComoCaixaFisico: true, requerConfirmacaoRecebimento: false);

        var caixa = await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 40m, estoque: 10);
        await db.CriarVendaService().ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 40m)],
            [new PagamentoEntrada(Formas.Pix, 40m)]);

        Assert.Equal(40m, await caixaService.ObterSaldoEmDinheiroAsync(caixa.Id));
    }
}
