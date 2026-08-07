using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

public class FormaPagamentoServiceTests
{
    [Fact]
    public async Task Seed_TrazAsQuatroFormasComAsFlagsCertas()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarFormaPagamentoService();

        var formas = await service.ListarAsync();

        // Regra 23: só o dinheiro ocupa a gaveta. Regra 21: só o Mercado Livre tem repasse posterior.
        Assert.Equal(
            ["Dinheiro"],
            formas.Where(f => f.ContaComoCaixaFisico).Select(f => f.Nome));
        Assert.Equal(
            ["Mercado Livre"],
            formas.Where(f => f.RequerConfirmacaoRecebimento).Select(f => f.Nome));
    }

    [Fact]
    public async Task ListarAsync_OmiteInativasPorPadrao()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarFormaPagamentoService();
        await service.DesativarAsync(Formas.Pix);

        Assert.DoesNotContain(await service.ListarAsync(), f => f.Id == Formas.Pix);
        Assert.Contains(await service.ListarAsync(incluirInativas: true), f => f.Id == Formas.Pix);
    }

    /// <summary>Regra 24: forma usada em venda só pode ser desativada, nunca excluída.</summary>
    [Fact]
    public async Task ExcluirAsync_EhRecusado_QuandoHaVendaComAForma_MasDesativarEhPermitido()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarFormaPagamentoService();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        await db.CriarVendaService().ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(Formas.Dinheiro, 10m)]);

        await Assert.ThrowsAsync<FormaPagamentoReferenciadaException>(() => service.ExcluirAsync(Formas.Dinheiro));

        await service.DesativarAsync(Formas.Dinheiro);
        var dinheiro = await service.ObterPorIdAsync(Formas.Dinheiro);
        Assert.False(dinheiro!.Ativo);
    }

    [Fact]
    public async Task ExcluirAsync_RemoveFormaNuncaUsada()
    {
        using var db = new TestDbContextFactory();
        var service = db.CriarFormaPagamentoService();
        var forma = await service.CriarAsync("Shopee", contaComoCaixaFisico: false, requerConfirmacaoRecebimento: true);

        await service.ExcluirAsync(forma.Id);

        Assert.Null(await service.ObterPorIdAsync(forma.Id));
    }

    /// <summary>Regra 17: cadastro é do Admin — nem o Operador mexe.</summary>
    [Fact]
    public async Task Operador_NaoMexeEmFormaDePagamento()
    {
        using var db = new TestDbContextFactory();
        var comoOperador = db.CriarFormaPagamentoService(Role.Operador);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.CriarAsync("Outra", false, false));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.AtualizarAsync(Formas.Pix, "Renomeada", false, false));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.DesativarAsync(Formas.Pix));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.ExcluirAsync(Formas.Pix));
    }

    [Fact]
    public async Task ConfirmarVendaAsync_RecusaFormaInativa()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        await db.CriarFormaPagamentoService().DesativarAsync(Formas.Pix);

        await Assert.ThrowsAsync<FormaPagamentoInvalidaException>(() =>
            db.CriarVendaService().ConfirmarVendaAsync(
                [new ItemVendaEntrada(produto.Id, 1, 10m)],
                [new PagamentoEntrada(Formas.Pix, 10m)]));
    }
}
