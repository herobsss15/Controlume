using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public record ResumoFormaPagamento(TipoPagamento TipoPagamento, decimal Total);

public record ResumoCaixa(
    int FechamentoCaixaId,
    DateTime DataAbertura,
    DateTime? DataFechamento,
    decimal ValorInicial,
    int QuantidadeVendas,
    decimal TotalVendas,
    IReadOnlyList<ResumoFormaPagamento> PorFormaPagamento);

public class CaixaService(ControlumeDbContext db)
{
    public async Task<FechamentoCaixa?> ObterAbertoAsync()
        => await db.FechamentosCaixa.AsNoTracking().FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto);

    /// <summary>Regra 2: só um caixa aberto por vez.</summary>
    public async Task<FechamentoCaixa> AbrirCaixaAsync(decimal valorInicial)
    {
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

    /// <summary>Regra 9: total sempre calculado a partir de Venda/VendaPagamento, nunca de um contador em cache.</summary>
    public async Task<ResumoCaixa> ObterResumoAsync(int caixaId)
    {
        var caixa = await db.FechamentosCaixa.AsNoTracking().FirstAsync(c => c.Id == caixaId);

        var vendas = await db.Vendas.AsNoTracking()
            .Where(v => v.FechamentoCaixaId == caixaId)
            .ToListAsync();

        var porFormaPagamento = await db.VendaPagamentos.AsNoTracking()
            .Where(p => p.Venda!.FechamentoCaixaId == caixaId)
            .GroupBy(p => p.TipoPagamento)
            .Select(g => new ResumoFormaPagamento(g.Key, g.Sum(p => p.Valor)))
            .ToListAsync();

        return new ResumoCaixa(
            caixa.Id,
            caixa.DataAbertura,
            caixa.DataFechamento,
            caixa.ValorInicial,
            vendas.Count,
            vendas.Sum(v => v.ValorTotal),
            porFormaPagamento);
    }

    /// <summary>Regra 9: registra DataFechamento e bloqueia novas vendas para este caixa.</summary>
    public async Task FecharCaixaAsync(int id)
    {
        var caixa = await db.FechamentosCaixa.FirstAsync(c => c.Id == id);
        caixa.DataFechamento = DateTime.UtcNow;
        caixa.Status = StatusCaixa.Fechado;
        await db.SaveChangesAsync();
    }

    public async Task<List<FechamentoCaixa>> ListarHistoricoAsync()
        => await db.FechamentosCaixa.AsNoTracking()
            .OrderByDescending(c => c.DataAbertura)
            .ToListAsync();
}
