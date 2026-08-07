using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

/// <summary>Regra 17: leitura livre para qualquer papel, escrita só para Admin.</summary>
public class FormaPagamentoService(ControlumeDbContext db, IUsuarioAtual usuarioAtual)
{
    public async Task<List<FormaPagamento>> ListarAsync(bool incluirInativas = false)
    {
        var query = db.FormasPagamento.AsNoTracking();
        if (!incluirInativas)
        {
            query = query.Where(f => f.Ativo);
        }
        return await query.OrderBy(f => f.Nome).ToListAsync();
    }

    public async Task<FormaPagamento?> ObterPorIdAsync(int id)
        => await db.FormasPagamento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

    public async Task<FormaPagamento> CriarAsync(string nome, bool contaComoCaixaFisico, bool requerConfirmacaoRecebimento)
    {
        await usuarioAtual.GarantirAdminAsync();

        var forma = new FormaPagamento
        {
            Nome = nome,
            Ativo = true,
            ContaComoCaixaFisico = contaComoCaixaFisico,
            RequerConfirmacaoRecebimento = requerConfirmacaoRecebimento
        };
        db.FormasPagamento.Add(forma);
        await db.SaveChangesAsync();
        return forma;
    }

    public async Task AtualizarAsync(int id, string nome, bool contaComoCaixaFisico, bool requerConfirmacaoRecebimento)
    {
        await usuarioAtual.GarantirAdminAsync();

        var forma = await db.FormasPagamento.FirstAsync(f => f.Id == id);
        forma.Nome = nome;
        forma.ContaComoCaixaFisico = contaComoCaixaFisico;
        forma.RequerConfirmacaoRecebimento = requerConfirmacaoRecebimento;
        await db.SaveChangesAsync();
    }

    public async Task DesativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var forma = await db.FormasPagamento.FirstAsync(f => f.Id == id);
        forma.Ativo = false;
        await db.SaveChangesAsync();
    }

    public async Task ReativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var forma = await db.FormasPagamento.FirstAsync(f => f.Id == id);
        forma.Ativo = true;
        await db.SaveChangesAsync();
    }

    /// <summary>Regra 24: só exclui se nenhum VendaPagamento referenciar a forma.</summary>
    public async Task ExcluirAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var forma = await db.FormasPagamento.FirstAsync(f => f.Id == id);

        var referenciada = await db.VendaPagamentos.AnyAsync(p => p.FormaPagamentoId == id);
        if (referenciada)
        {
            throw new FormaPagamentoReferenciadaException(forma.Nome);
        }

        db.FormasPagamento.Remove(forma);
        await db.SaveChangesAsync();
    }
}
