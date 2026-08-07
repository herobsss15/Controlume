using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

public class SangriaServiceTests
{
    [Fact]
    public async Task RegistrarAsync_LancaExcecao_QuandoNaoHaCaixaAberto()
    {
        using var db = new TestDbContextFactory();
        var sangriaService = db.CriarSangriaService();

        await Assert.ThrowsAsync<NenhumCaixaAbertoException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Compra, 10m, null));
    }

    [Fact]
    public async Task RegistrarAsync_LancaExcecao_QuandoCaixaJaFoiFechado()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var sangriaService = db.CriarSangriaService();
        var caixa = await caixaService.AbrirCaixaAsync(100m);
        await caixaService.FecharCaixaAsync(caixa.Id);

        await Assert.ThrowsAsync<NenhumCaixaAbertoException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Compra, 10m, null));
    }

    /// <summary>Regra 13: o motivo é obrigatório, então um valor fora do enum não passa.</summary>
    [Fact]
    public async Task RegistrarAsync_LancaExcecao_QuandoMotivoNaoEhValido()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(100m);
        var sangriaService = db.CriarSangriaService();

        await Assert.ThrowsAsync<SangriaSemMotivoException>(() =>
            sangriaService.RegistrarAsync((MotivoSangria)99, 10m, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RegistrarAsync_LancaExcecao_QuandoValorNaoEhPositivo(decimal valor)
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(100m);
        var sangriaService = db.CriarSangriaService();

        await Assert.ThrowsAsync<ValorSangriaInvalidoException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Pagamento, valor, null));
    }

    /// <summary>Regra 12: o limite é o saldo em dinheiro do momento, não só o ValorInicial.</summary>
    [Fact]
    public async Task RegistrarAsync_LancaExcecao_QuandoDeixariaSaldoNegativo()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var sangriaService = db.CriarSangriaService();

        await caixaService.AbrirCaixaAsync(100m);
        var produto = await db.SeedProdutoAsync(preco: 50m, estoque: 10);
        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 50m)],
            [new PagamentoEntrada(TipoPagamento.Dinheiro, 50m)]);

        // Saldo em dinheiro = 100 + 50 = 150.
        await sangriaService.RegistrarAsync(MotivoSangria.Compra, 150m, null);
        await Assert.ThrowsAsync<SaldoInsuficienteParaSangriaException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Compra, 0.01m, null));
    }

    [Fact]
    public async Task RegistrarAsync_DescontaSangriasAnteriores_AoValidarOSaldo()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(100m);
        var sangriaService = db.CriarSangriaService();

        await sangriaService.RegistrarAsync(MotivoSangria.Pagamento, 60m, null);

        await Assert.ThrowsAsync<SaldoInsuficienteParaSangriaException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Pagamento, 50m, null));
    }

    /// <summary>Cartão e Pix não ocupam a gaveta: não aumentam o quanto dá para sangrar.</summary>
    [Fact]
    public async Task RegistrarAsync_NaoContaVendasEmCartaoOuPix_NoSaldoDisponivel()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService();
        var vendaService = db.CriarVendaService();
        var sangriaService = db.CriarSangriaService();

        await caixaService.AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 100m, estoque: 10);
        await vendaService.ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 100m)],
            [new PagamentoEntrada(TipoPagamento.Cartao, 60m), new PagamentoEntrada(TipoPagamento.Pix, 40m)]);

        await Assert.ThrowsAsync<SaldoInsuficienteParaSangriaException>(() =>
            sangriaService.RegistrarAsync(MotivoSangria.Compra, 10m, null));
    }

    [Fact]
    public async Task RegistrarAsync_GravaValorMotivoEDescricao()
    {
        using var db = new TestDbContextFactory();
        var caixa = await db.CriarCaixaService().AbrirCaixaAsync(200m);
        var sangriaService = db.CriarSangriaService();

        await sangriaService.RegistrarAsync(MotivoSangria.Pagamento, 80m, "  pagamento fornecedor X  ");

        var sangria = Assert.Single(await sangriaService.ListarPorCaixaAsync(caixa.Id));
        Assert.Equal(caixa.Id, sangria.FechamentoCaixaId);
        Assert.Equal(80m, sangria.Valor);
        Assert.Equal(MotivoSangria.Pagamento, sangria.Motivo);
        Assert.Equal("pagamento fornecedor X", sangria.Descricao);
    }

    [Fact]
    public async Task RegistrarAsync_DeixaDescricaoNula_QuandoNaoInformada()
    {
        using var db = new TestDbContextFactory();
        var caixa = await db.CriarCaixaService().AbrirCaixaAsync(50m);
        var sangriaService = db.CriarSangriaService();

        await sangriaService.RegistrarAsync(MotivoSangria.Compra, 10m, "   ");

        var sangria = Assert.Single(await sangriaService.ListarPorCaixaAsync(caixa.Id));
        Assert.Null(sangria.Descricao);
    }
}
