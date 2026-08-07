using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public record ResumoFormaPagamento(int FormaPagamentoId, string Nome, bool ContaComoCaixaFisico, decimal Total);

public record ResumoCaixa(
    int FechamentoCaixaId,
    DateTime DataAbertura,
    DateTime? DataFechamento,
    decimal ValorInicial,
    int QuantidadeVendas,
    decimal TotalVendas,
    IReadOnlyList<ResumoFormaPagamento> PorFormaPagamento,
    decimal TotalSangrias,
    decimal SaldoEmDinheiro,
    decimal? SaldoFinal);

public class CaixaService(ControlumeDbContext db, IUsuarioAtual usuarioAtual)
{
    public async Task<FechamentoCaixa?> ObterAbertoAsync()
        => await db.FechamentosCaixa.AsNoTracking().FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto);

    /// <summary>Regra 2: só um caixa aberto por vez.</summary>
    public async Task<FechamentoCaixa> AbrirCaixaAsync(decimal valorInicial)
    {
        await usuarioAtual.GarantirPodeEscreverAsync();

        if (await db.FechamentosCaixa.AnyAsync(c => c.Status == StatusCaixa.Aberto))
        {
            throw new CaixaJaAbertoException();
        }

        var caixa = new FechamentoCaixa
        {
            DataAbertura = DateTime.UtcNow,
            ValorInicial = valorInicial,
            Status = StatusCaixa.Aberto
        };
        db.FechamentosCaixa.Add(caixa);
        await db.SaveChangesAsync();
        return caixa;
    }

    /// <summary>
    /// Regra 15: sugestão de ValorInicial para a próxima abertura, vinda do SaldoFinal do último
    /// caixa fechado. É só sugestão — a contagem física manda, então a tela deixa editar.
    /// </summary>
    public async Task<decimal> ObterSugestaoValorInicialAsync()
        => await db.FechamentosCaixa.AsNoTracking()
            .Where(c => c.Status == StatusCaixa.Fechado && c.SaldoFinal != null)
            .OrderByDescending(c => c.DataFechamento)
            .Select(c => c.SaldoFinal!.Value)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Regra 9: total sempre calculado a partir de Venda/VendaPagamento, nunca de um contador em
    /// cache. Regra 28: venda cancelada fica de fora de tudo, como se não tivesse acontecido.
    /// </summary>
    public async Task<ResumoCaixa> ObterResumoAsync(int caixaId)
    {
        var caixa = await db.FechamentosCaixa.AsNoTracking().FirstAsync(c => c.Id == caixaId);

        var vendas = await db.Vendas.AsNoTracking()
            .Where(v => v.FechamentoCaixaId == caixaId && !v.Cancelada)
            .ToListAsync();

        // O agrupamento é só pela FK — agrupar direto por campos da forma não é traduzível. Nome e
        // flag entram depois, da tabela inteira, que tem meia dúzia de linhas.
        var totaisPorForma = await db.VendaPagamentos.AsNoTracking()
            .Where(p => p.Venda!.FechamentoCaixaId == caixaId && !p.Venda.Cancelada)
            .GroupBy(p => p.FormaPagamentoId)
            .Select(g => new { FormaPagamentoId = g.Key, Total = g.Sum(p => p.Valor) })
            .ToListAsync();

        var formas = await db.FormasPagamento.AsNoTracking().ToDictionaryAsync(f => f.Id);

        var porFormaPagamento = totaisPorForma
            .Select(t => new ResumoFormaPagamento(
                t.FormaPagamentoId,
                formas[t.FormaPagamentoId].Nome,
                formas[t.FormaPagamentoId].ContaComoCaixaFisico,
                t.Total))
            .OrderBy(f => f.Nome)
            .ToList();

        var totalSangrias = await db.Sangrias.AsNoTracking()
            .Where(s => s.FechamentoCaixaId == caixaId)
            .SumAsync(s => (decimal?)s.Valor) ?? 0m;

        // Regras 14 e 23: só o que ocupa a gaveta entra na conta, e quem decide isso é a flag da
        // forma de pagamento — não mais uma comparação com o nome "Dinheiro".
        var totalEmDinheiro = porFormaPagamento
            .Where(f => f.ContaComoCaixaFisico)
            .Sum(f => f.Total);

        return new ResumoCaixa(
            caixa.Id,
            caixa.DataAbertura,
            caixa.DataFechamento,
            caixa.ValorInicial,
            vendas.Count,
            vendas.Sum(v => v.ValorTotal),
            porFormaPagamento,
            totalSangrias,
            caixa.ValorInicial + totalEmDinheiro - totalSangrias,
            caixa.SaldoFinal);
    }

    /// <summary>Regras 12 e 14: saldo físico corrente da gaveta (inicial + vendas em dinheiro − sangrias).</summary>
    public async Task<decimal> ObterSaldoEmDinheiroAsync(int caixaId)
        => (await ObterResumoAsync(caixaId)).SaldoEmDinheiro;

    /// <summary>
    /// Regra 9: registra DataFechamento e bloqueia novas vendas para este caixa.
    /// Regra 14: congela o SaldoFinal do período, já descontadas as sangrias.
    /// </summary>
    public async Task FecharCaixaAsync(int id)
    {
        await usuarioAtual.GarantirPodeEscreverAsync();

        var caixa = await db.FechamentosCaixa.FirstAsync(c => c.Id == id);
        caixa.SaldoFinal = await ObterSaldoEmDinheiroAsync(id);
        caixa.DataFechamento = DateTime.UtcNow;
        caixa.Status = StatusCaixa.Fechado;
        await db.SaveChangesAsync();
    }

    public async Task<List<FechamentoCaixa>> ListarHistoricoAsync()
        => await db.FechamentosCaixa.AsNoTracking()
            .OrderByDescending(c => c.DataAbertura)
            .ToListAsync();
}
