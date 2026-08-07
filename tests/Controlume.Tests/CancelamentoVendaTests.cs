using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

/// <summary>Regras 25 a 30: cancelamento lógico, estoque de volta e caixa intocado.</summary>
public class CancelamentoVendaTests
{
    /// <summary>Abre caixa, vende <paramref name="quantidade"/> itens e devolve a venda e o produto.</summary>
    private static async Task<(FechamentoCaixa Caixa, Venda Venda, Produto Produto)> SeedVendaAsync(
        TestDbContextFactory db, int estoque = 5, int quantidade = 2, int formaId = Formas.Dinheiro)
    {
        var caixa = await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: estoque);
        var venda = await db.CriarVendaService().ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, quantidade, 10m)],
            [new PagamentoEntrada(formaId, 10m * quantidade)]);
        return (caixa, venda, produto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cancelar_EhRecusado_SemMotivo(string motivo)
    {
        using var db = new TestDbContextFactory();
        var (_, venda, produto) = await SeedVendaAsync(db);

        await Assert.ThrowsAsync<MotivoCancelamentoObrigatorioException>(
            () => db.CriarVendaService().CancelarVendaAsync(venda.Id, motivo));

        // Regra 25: recusado é recusado — nem a venda nem o estoque se mexem.
        var detalhe = await db.CriarVendaService().ObterDetalheAsync(venda.Id);
        Assert.False(detalhe!.Cancelada);
        Assert.Equal(3, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);
    }

    /// <summary>Regras 26 e 27: some do financeiro, não do histórico; e o estoque volta.</summary>
    [Fact]
    public async Task Cancelar_MarcaAVendaRestauraOEstoqueEMantemNoHistorico()
    {
        using var db = new TestDbContextFactory();
        var vendaService = db.CriarVendaService();
        var (_, venda, produto) = await SeedVendaAsync(db, estoque: 5, quantidade: 2);
        Assert.Equal(3, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);

        await vendaService.CancelarVendaAsync(venda.Id, "  cliente desistiu  ");

        var detalhe = await vendaService.ObterDetalheAsync(venda.Id);
        Assert.True(detalhe!.Cancelada);
        Assert.Equal("cliente desistiu", detalhe.MotivoCancelamento);
        Assert.NotNull(detalhe.DataCancelamento);
        Assert.Equal(5, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);

        var historico = await vendaService.ListarHistoricoAsync();
        Assert.True(historico.Single(v => v.Id == venda.Id).Cancelada);
    }

    [Fact]
    public async Task Cancelar_RegistraQuemCancelou()
    {
        using var db = new TestDbContextFactory();
        var usuario = await db.SeedUsuarioAsync(Role.Admin, "admin.teste");
        var (_, venda, _) = await SeedVendaAsync(db);

        var comoAdminLogado = new VendaService(db.Context, new UsuarioAtualFake(Role.Admin, usuario.Id));
        await comoAdminLogado.CancelarVendaAsync(venda.Id, "erro de digitação");

        var detalhe = await comoAdminLogado.ObterDetalheAsync(venda.Id);
        Assert.Equal(usuario.Id, detalhe!.CanceladoPorUsuarioId);
        Assert.Equal("admin.teste", detalhe.CanceladoPorUsuario!.Nome);
    }

    [Fact]
    public async Task Cancelar_EhRecusado_QuandoAVendaJaEstaCancelada()
    {
        using var db = new TestDbContextFactory();
        var vendaService = db.CriarVendaService();
        var (_, venda, produto) = await SeedVendaAsync(db, estoque: 5, quantidade: 2);
        await vendaService.CancelarVendaAsync(venda.Id, "primeiro cancelamento");

        await Assert.ThrowsAsync<VendaJaCanceladaException>(
            () => vendaService.CancelarVendaAsync(venda.Id, "segundo cancelamento"));

        // O estoque não pode ser devolvido duas vezes.
        Assert.Equal(5, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);
    }

    /// <summary>Regra 28: no caixa aberto, o cancelamento sai do saldo na hora.</summary>
    [Fact]
    public async Task Cancelar_TiraAVendaDoResumoEDoSaldoDoCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var caixa = await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);
        var vendaService = db.CriarVendaService();
        var cancelada = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 50m)],
            [new PagamentoEntrada(Formas.Dinheiro, 50m)]);
        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 30m)],
            [new PagamentoEntrada(Formas.Dinheiro, 30m)]);

        await vendaService.CancelarVendaAsync(cancelada.Id, "cobrado errado");

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);
        Assert.Equal(1, resumo.QuantidadeVendas);
        Assert.Equal(30m, resumo.TotalVendas);
        Assert.Equal(30m, resumo.PorFormaPagamento.Single().Total);
        Assert.Equal(130m, resumo.SaldoEmDinheiro); // 100 inicial + 30 da venda que sobrou
    }

    /// <summary>Regra 12 combinada com a 28: o dinheiro cancelado não sustenta mais uma sangria.</summary>
    [Fact]
    public async Task Cancelar_ReduzOLimiteDaSangria()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 80m, estoque: 10);
        var vendaService = db.CriarVendaService();
        var venda = await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 80m)],
            [new PagamentoEntrada(Formas.Dinheiro, 80m)]);

        await vendaService.CancelarVendaAsync(venda.Id, "venda registrada em duplicidade");

        await Assert.ThrowsAsync<SaldoInsuficienteParaSangriaException>(
            () => db.CriarSangriaService().RegistrarAsync(MotivoSangria.Compra, 80m, null));
    }

    /// <summary>
    /// Regra 30: o SaldoFinal é histórico congelado no fechamento. Cancelar depois não o reescreve,
    /// mesmo que o recálculo de hoje já mostre outro número.
    /// </summary>
    [Fact]
    public async Task Cancelar_NaoReescreveOSaldoFinalDeCaixaJaFechado()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var caixa = await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);
        var venda = await db.CriarVendaService().ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 50m)],
            [new PagamentoEntrada(Formas.Dinheiro, 50m)]);
        await caixaService.FecharCaixaAsync(caixa.Id);
        Assert.Equal(150m, (await caixaService.ObterResumoAsync(caixa.Id)).SaldoFinal);

        await db.CriarVendaService().CancelarVendaAsync(venda.Id, "estorno pedido pelo cliente");

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);
        Assert.Equal(150m, resumo.SaldoFinal);      // congelado, não mudou
        Assert.Equal(100m, resumo.SaldoEmDinheiro); // recalculado sem a venda: é a divergência da regra 30
        Assert.Equal(0, resumo.QuantidadeVendas);
        Assert.Equal(150m, await caixaService.ObterSugestaoValorInicialAsync());
    }

    /// <summary>Regra 29: o Operador desfaz o que ainda está no caixa corrente.</summary>
    [Fact]
    public async Task Operador_CancelaVendaDeCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var (_, venda, produto) = await SeedVendaAsync(db, estoque: 5, quantidade: 2);

        await db.CriarVendaService(Role.Operador).CancelarVendaAsync(venda.Id, "troco errado");

        Assert.True((await db.CriarVendaService().ObterDetalheAsync(venda.Id))!.Cancelada);
        Assert.Equal(5, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);
    }

    /// <summary>Regra 29: caixa fechado é território de Admin — e o Operador não deixa rastro ao tentar.</summary>
    [Fact]
    public async Task Operador_NaoCancelaVendaDeCaixaJaFechado_MasAdminCancela()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var (caixa, venda, produto) = await SeedVendaAsync(db, estoque: 5, quantidade: 2);
        await caixaService.FecharCaixaAsync(caixa.Id);

        await Assert.ThrowsAsync<CancelamentoDeCaixaFechadoException>(
            () => db.CriarVendaService(Role.Operador).CancelarVendaAsync(venda.Id, "devolução"));
        Assert.False((await db.CriarVendaService().ObterDetalheAsync(venda.Id))!.Cancelada);
        Assert.Equal(3, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);

        await db.CriarVendaService(Role.Admin).CancelarVendaAsync(venda.Id, "devolução");

        Assert.True((await db.CriarVendaService().ObterDetalheAsync(venda.Id))!.Cancelada);
        Assert.Equal(5, (await db.CriarProdutoService().ObterPorIdAsync(produto.Id))!.QuantidadeEstoque);
    }

    /// <summary>Regras 18 e 29: o Stakeholder não cancela nada, com o caixa aberto ou fechado.</summary>
    [Theory]
    [InlineData(Role.Stakeholder)]
    [InlineData(null)]
    public async Task Stakeholder_ENaoAutenticado_NuncaCancelam(Role? role)
    {
        using var db = new TestDbContextFactory();
        var (caixa, venda, _) = await SeedVendaAsync(db);

        await Assert.ThrowsAsync<AcessoNegadoException>(
            () => db.CriarVendaService(role).CancelarVendaAsync(venda.Id, "qualquer motivo"));

        await db.CriarCaixaService().FecharCaixaAsync(caixa.Id);
        await Assert.ThrowsAsync<AcessoNegadoException>(
            () => db.CriarVendaService(role).CancelarVendaAsync(venda.Id, "qualquer motivo"));
    }

    /// <summary>Regra 28: pagamento pendente de uma venda cancelada não é mais confirmável.</summary>
    [Fact]
    public async Task MarcarComoRecebido_EhRecusado_QuandoAVendaFoiCancelada()
    {
        using var db = new TestDbContextFactory();
        var vendaService = db.CriarVendaService();
        var (_, venda, _) = await SeedVendaAsync(db, quantidade: 2, formaId: Formas.MercadoLivre);
        var pagamentoId = (await vendaService.ObterDetalheAsync(venda.Id))!.Pagamentos.Single().Id;
        await vendaService.CancelarVendaAsync(venda.Id, "pedido cancelado no anúncio");

        await Assert.ThrowsAsync<VendaJaCanceladaException>(
            () => vendaService.MarcarPagamentoComoRecebidoAsync(pagamentoId));
    }
}
