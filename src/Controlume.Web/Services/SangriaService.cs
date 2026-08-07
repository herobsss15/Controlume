using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public class SangriaService(ControlumeDbContext db, CaixaService caixaService, IUsuarioAtual usuarioAtual)
{
    /// <summary>
    /// Registra uma retirada de dinheiro da gaveta: exige caixa aberto (regra 11), motivo
    /// (regra 13) e saldo em dinheiro suficiente no momento da retirada (regra 12).
    /// </summary>
    public async Task<Sangria> RegistrarAsync(MotivoSangria motivo, decimal valor, string? descricao)
    {
        await usuarioAtual.GarantirPodeEscreverAsync();

        if (!Enum.IsDefined(motivo))
        {
            throw new SangriaSemMotivoException();
        }

        if (valor <= 0)
        {
            throw new ValorSangriaInvalidoException();
        }

        // A validação de saldo e a inserção precisam enxergar o mesmo estado: sem a transação,
        // duas sangrias simultâneas passariam pela regra 12 e juntas estourariam a gaveta.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var caixaAberto = await db.FechamentosCaixa.FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto);
        if (caixaAberto is null)
        {
            throw new NenhumCaixaAbertoException();
        }

        var saldoDisponivel = await caixaService.ObterSaldoEmDinheiroAsync(caixaAberto.Id);
        if (valor > saldoDisponivel)
        {
            throw new SaldoInsuficienteParaSangriaException(valor, saldoDisponivel);
        }

        var sangria = new Sangria
        {
            FechamentoCaixaId = caixaAberto.Id,
            DataHora = DateTime.UtcNow,
            Valor = valor,
            Motivo = motivo,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim()
        };
        db.Sangrias.Add(sangria);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return sangria;
    }

    public async Task<List<Sangria>> ListarPorCaixaAsync(int caixaId)
        => await db.Sangrias.AsNoTracking()
            .Where(s => s.FechamentoCaixaId == caixaId)
            .OrderByDescending(s => s.DataHora)
            .ToListAsync();
}
