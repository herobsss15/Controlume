using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public record ItemVendaEntrada(int ProdutoId, int Quantidade, decimal PrecoVenda);

public record PagamentoEntrada(int FormaPagamentoId, decimal Valor);

public record VendaResumo(int Id, DateTime DataHora, decimal ValorTotal, int QuantidadeItens, bool Cancelada);

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

        // A tela pode estar com um cadastro velho na mão: reconfere que a forma existe e segue ativa
        // antes de gravar, porque é dela que sai a regra 21.
        var formaIds = pagamentos.Select(p => p.FormaPagamentoId).Distinct().ToList();
        var formas = await db.FormasPagamento
            .Where(f => formaIds.Contains(f.Id) && f.Ativo)
            .ToDictionaryAsync(f => f.Id);
        if (formas.Count != formaIds.Count)
        {
            throw new FormaPagamentoInvalidaException();
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

        var agora = DateTime.UtcNow;
        var venda = new Venda
        {
            FechamentoCaixaId = caixaAberto.Id,
            DataHora = agora,
            ValorTotal = valorTotal,
            Itens = itensVenda,
            // Regra 21: só as formas com repasse posterior nascem aguardando confirmação.
            Pagamentos = pagamentos.Select(p => new VendaPagamento
            {
                FormaPagamentoId = p.FormaPagamentoId,
                Valor = p.Valor,
                Recebido = !formas[p.FormaPagamentoId].RequerConfirmacaoRecebimento,
                DataRecebimento = formas[p.FormaPagamentoId].RequerConfirmacaoRecebimento ? null : agora
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

    /// <summary>
    /// Regra 22: confirma o repasse de um pagamento que nasceu pendente. Não existe o inverso —
    /// esta é a única porta para o campo, e ela só anda para frente.
    /// </summary>
    public async Task MarcarPagamentoComoRecebidoAsync(int vendaPagamentoId)
    {
        await usuarioAtual.GarantirPodeEscreverAsync();

        var pagamento = await db.VendaPagamentos
            .Include(p => p.Venda)
            .FirstAsync(p => p.Id == vendaPagamentoId);

        if (pagamento.Recebido)
        {
            throw new PagamentoJaRecebidoException();
        }

        // Regra 28: venda cancelada não movimenta mais nada — nem o repasse pendente dela.
        if (pagamento.Venda!.Cancelada)
        {
            throw new VendaJaCanceladaException(pagamento.VendaId);
        }

        pagamento.Recebido = true;
        pagamento.DataRecebimento = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Regras 25 a 30: delete lógico com motivo obrigatório, estoque devolvido e a venda fora de
    /// todo cálculo financeiro daqui para frente. Quem pode cancelar depende do papel e do estado
    /// do caixa daquela venda; o SaldoFinal já congelado de um caixa fechado não é reescrito.
    /// </summary>
    public async Task CancelarVendaAsync(int vendaId, string motivo)
    {
        // Regra 29, primeira metade: Stakeholder (e anônimo) não cancela em nenhuma situação.
        await usuarioAtual.GarantirPodeEscreverAsync();

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new MotivoCancelamentoObrigatorioException();
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var venda = await db.Vendas
            .Include(v => v.Itens)
            .Include(v => v.FechamentoCaixa)
            .FirstAsync(v => v.Id == vendaId);

        if (venda.Cancelada)
        {
            throw new VendaJaCanceladaException(vendaId);
        }

        // Regra 29, segunda metade: caixa já fechado é território de Admin — o Operador só desfaz
        // o que ainda está no caixa corrente, onde o saldo se recalcula sozinho.
        if (venda.FechamentoCaixa!.Status == StatusCaixa.Fechado && !await usuarioAtual.EhAdminAsync())
        {
            throw new CancelamentoDeCaixaFechadoException();
        }

        // Regra 27: reverso exato da regra 6.
        var produtoIds = venda.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var item in venda.Itens)
        {
            produtos[item.ProdutoId].QuantidadeEstoque += item.Quantidade;
        }

        venda.Cancelada = true;
        venda.MotivoCancelamento = motivo.Trim();
        venda.DataCancelamento = DateTime.UtcNow;
        venda.CanceladoPorUsuarioId = await usuarioAtual.ObterIdAsync();

        // Regra 30: FechamentoCaixa.SaldoFinal fica como está. É um dado histórico congelado no
        // fechamento; a diferença que sobrar na gaveta se acerta na próxima abertura/sangria.
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<VendaResumo>> ListarHistoricoAsync()
        => await db.Vendas.AsNoTracking()
            .OrderByDescending(v => v.DataHora)
            .Select(v => new VendaResumo(v.Id, v.DataHora, v.ValorTotal, v.Itens.Count, v.Cancelada))
            .ToListAsync();

    public async Task<Venda?> ObterDetalheAsync(int id)
        => await db.Vendas.AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos).ThenInclude(p => p.FormaPagamento)
            .Include(v => v.FechamentoCaixa)
            .Include(v => v.CanceladoPorUsuario)
            .FirstOrDefaultAsync(v => v.Id == id);
}
