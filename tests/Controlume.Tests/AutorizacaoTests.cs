using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Exceptions;
using Xunit;

namespace Controlume.Tests;

/// <summary>
/// Regras 17 e 18 na camada de serviço: esconder o botão não basta, a chamada direta
/// também precisa ser recusada.
/// </summary>
public class AutorizacaoTests
{
    /// <summary>Sem papel de escrita — inclusive sem usuário nenhum — nada é gravado.</summary>
    [Theory]
    [InlineData(Role.Stakeholder)]
    [InlineData(null)]
    public async Task AbrirCaixa_EhRecusado_ParaStakeholderEParaAnonimo(Role? role)
    {
        using var db = new TestDbContextFactory();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => db.CriarCaixaService(role).AbrirCaixaAsync(100m));
    }

    [Fact]
    public async Task Stakeholder_NaoAbreNemFechaCaixa()
    {
        using var db = new TestDbContextFactory();
        var caixa = await db.CriarCaixaService().AbrirCaixaAsync(100m);
        var comoStakeholder = db.CriarCaixaService(Role.Stakeholder);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoStakeholder.AbrirCaixaAsync(50m));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoStakeholder.FecharCaixaAsync(caixa.Id));
    }

    [Fact]
    public async Task Stakeholder_NaoConfirmaVenda()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(0m);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            db.CriarVendaService(Role.Stakeholder).ConfirmarVendaAsync(
                [new ItemVendaEntrada(produto.Id, 1, 10m)],
                [new PagamentoEntrada(TipoPagamento.Dinheiro, 10m)]));
    }

    [Fact]
    public async Task Stakeholder_NaoRegistraSangria()
    {
        using var db = new TestDbContextFactory();
        await db.CriarCaixaService().AbrirCaixaAsync(100m);

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            db.CriarSangriaService(Role.Stakeholder).RegistrarAsync(MotivoSangria.Compra, 10m, null));
    }

    [Fact]
    public async Task Stakeholder_LeSemRestricao()
    {
        using var db = new TestDbContextFactory();
        var caixa = await db.CriarCaixaService().AbrirCaixaAsync(100m);
        await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        var resumo = await db.CriarCaixaService(Role.Stakeholder).ObterResumoAsync(caixa.Id);
        var produtos = await db.CriarProdutoService(Role.Stakeholder).ListarAsync();
        var tipos = await db.CriarTipoProdutoService(Role.Stakeholder).ListarAsync();

        Assert.Equal(100m, resumo.SaldoEmDinheiro);
        Assert.NotEmpty(produtos);
        Assert.NotEmpty(tipos);
    }

    /// <summary>Regra 17: Operador opera a loja, mas não mexe nos cadastros.</summary>
    [Fact]
    public async Task Operador_NaoCadastraNemEditaProduto()
    {
        using var db = new TestDbContextFactory();
        var produtoSeed = await db.SeedProdutoAsync(preco: 10m, estoque: 5);
        var comoOperador = db.CriarProdutoService(Role.Operador);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.CriarAsync(
            new Produto { Nome = "Novo", TipoProdutoId = 1, Preco = 10m, QuantidadeEstoque = 1 }));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.AtualizarAsync(
            new Produto { Id = produtoSeed.Id, Nome = "Editado", TipoProdutoId = 1, Preco = 20m, QuantidadeEstoque = 5 }));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.DesativarAsync(produtoSeed.Id));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.ReativarAsync(produtoSeed.Id));
    }

    [Fact]
    public async Task Operador_NaoMexeEmTipoProduto()
    {
        using var db = new TestDbContextFactory();
        var tipo = await db.CriarTipoProdutoService().CriarAsync("Fitas K7");
        var comoOperador = db.CriarTipoProdutoService(Role.Operador);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.CriarAsync("Outro"));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.AtualizarAsync(tipo.Id, "Renomeado"));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.DesativarAsync(tipo.Id));
        await Assert.ThrowsAsync<AcessoNegadoException>(() => comoOperador.ExcluirAsync(tipo.Id));
    }

    [Fact]
    public async Task Operador_VendeAbreCaixaESangra()
    {
        using var db = new TestDbContextFactory();
        var caixaService = db.CriarCaixaService(Role.Operador);
        var produto = await db.SeedProdutoAsync(preco: 10m, estoque: 5);

        var caixa = await caixaService.AbrirCaixaAsync(100m);
        await db.CriarVendaService(Role.Operador).ConfirmarVendaAsync(
            [new ItemVendaEntrada(produto.Id, 1, 10m)],
            [new PagamentoEntrada(TipoPagamento.Dinheiro, 10m)]);
        await db.CriarSangriaService(Role.Operador).RegistrarAsync(MotivoSangria.Pagamento, 30m, null);
        await caixaService.FecharCaixaAsync(caixa.Id);

        var resumo = await caixaService.ObterResumoAsync(caixa.Id);
        Assert.Equal(80m, resumo.SaldoFinal); // 100 + 10 − 30
    }
}
