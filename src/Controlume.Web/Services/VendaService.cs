using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public record ItemVendaEntrada(int ProdutoId, int Quantidade, decimal PrecoVenda);

public record PagamentoEntrada(TipoPagamento TipoPagamento, decimal Valor);

public record VendaResumo(int Id, DateTime DataHora, decimal ValorTotal, int QuantidadeItens);

public class VendaService(ControlumeDbContext db, IUsuarioAtual usuarioAtual)
{
    /// <summary>
    /// Confirma a venda dentro de uma única transação: valida caixa aberto (regra 1),
    /// estoque (regra 6) e soma de pagamentos (regra 3); congela PrecoTabela (regra 4)
    /// mantendo PrecoVenda independente (regra 5); decrementa estoque (regra 6).
    /// </summary>
    public async Task<Venda> ConfirmarVendaAsync(IReadOnlyList<ItemVendaEntrada> itens, IReadOnlyList<PagamentoEntrada> pagamentos)
    {
        await usuarioAtual.GarantirPodeEscreverAsync();

        if (itens.Count == 0)
        {
            throw new VendaSemItensException();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var caixaAberto = await db.FechamentosCaixa.FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto);
        if (caixaAberto is null)
        {
            throw new NenhumCaixaAbertoException();
        }

        var produtoIds = itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var faltantes = itens
            .Where(item => produtos[item.ProdutoId].QuantidadeEstoque < item.Quantidade)
            .Select(item => produtos[item.ProdutoId].Nome)
            .ToList();
        if (faltantes.Count > 0)
        {
            throw new EstoqueInsuficienteException(faltantes);
        }

        var itensVenda = itens.Select(i => new ItemVenda
        {
            ProdutoId = i.ProdutoId,
            Quantidade = i.Quantidade,
            PrecoTabela = produtos[i.ProdutoId].Preco,
            PrecoVenda = i.PrecoVenda
        }).ToList();

        var valorTotal = itensVenda.Sum(i => i.PrecoVenda * i.Quantidade);
        var totalPagamentos = pagamentos.Sum(p => p.Valor);
        if (totalPagamentos != valorTotal)
        {
            throw new PagamentoDivergenteException(valorTotal, totalPagamentos);
        }

        var venda = new Venda
        {
            FechamentoCaixaId = caixaAberto.Id,
            DataHora = DateTime.UtcNow,
            ValorTotal = valorTotal,
            Itens = itensVenda,
            Pagamentos = pagamentos.Select(p => new VendaPagamento
            {
                TipoPagamento = p.TipoPagamento,
                Valor = p.Valor
            }).ToList()
        };
        db.Vendas.Add(venda);

        foreach (var item in itensVenda)
        {
            produtos[item.ProdutoId].QuantidadeEstoque -= item.Quantidade;
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return venda;
    }

    public async Task<List<VendaResumo>> ListarHistoricoAsync()
        => await db.Vendas.AsNoTracking()
            .OrderByDescending(v => v.DataHora)
            .Select(v => new VendaResumo(v.Id, v.DataHora, v.ValorTotal, v.Itens.Count))
            .ToListAsync();

    public async Task<Venda?> ObterDetalheAsync(int id)
        => await db.Vendas.AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .FirstOrDefaultAsync(v => v.Id == id);
}
