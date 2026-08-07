using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;
using Controlume.Web.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

/// <summary>Regra 17: leitura livre para qualquer papel, escrita só para Admin.</summary>
public class TipoProdutoService(ControlumeDbContext db, IUsuarioAtual usuarioAtual)
{
    public async Task<List<TipoProduto>> ListarAsync(bool incluirInativos = false)
    {
        var query = db.TiposProduto.AsNoTracking();
        if (!incluirInativos)
        {
            query = query.Where(t => t.Ativo);
        }
        return await query.OrderBy(t => t.Nome).ToListAsync();
    }

    public async Task<TipoProduto?> ObterPorIdAsync(int id)
        => await db.TiposProduto.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TipoProduto> CriarAsync(string nome)
    {
        await usuarioAtual.GarantirAdminAsync();

        var tipo = new TipoProduto { Nome = nome, Ativo = true };
        db.TiposProduto.Add(tipo);
        await db.SaveChangesAsync();
        return tipo;
    }

    public async Task AtualizarAsync(int id, string nome)
    {
        await usuarioAtual.GarantirAdminAsync();

        var tipo = await db.TiposProduto.FirstAsync(t => t.Id == id);
        tipo.Nome = nome;
        await db.SaveChangesAsync();
    }

    public async Task DesativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var tipo = await db.TiposProduto.FirstAsync(t => t.Id == id);
        tipo.Ativo = false;
        await db.SaveChangesAsync();
    }

    public async Task ReativarAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var tipo = await db.TiposProduto.FirstAsync(t => t.Id == id);
        tipo.Ativo = true;
        await db.SaveChangesAsync();
    }

    /// <summary>Regra 8: só exclui se nenhum Produto (mesmo inativo) referenciar o tipo.</summary>
    public async Task ExcluirAsync(int id)
    {
        await usuarioAtual.GarantirAdminAsync();

        var tipo = await db.TiposProduto.FirstAsync(t => t.Id == id);

        var referenciado = await db.Produtos.AnyAsync(p => p.TipoProdutoId == id);
        if (referenciado)
        {
            throw new TipoProdutoReferenciadoException(tipo.Nome);
        }

        db.TiposProduto.Remove(tipo);
        await db.SaveChangesAsync();
    }
}
