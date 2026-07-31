using Controlume.Web.Data;
using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services;

public class ProdutoService(ControlumeDbContext db)
{
    public async Task<List<Produto>> ListarAsync(bool incluirInativos = false, int? tipoProdutoId = null)
    {
        var query = db.Produtos.Include(p => p.TipoProduto).AsNoTracking().AsQueryable();
        if (!incluirInativos)
        {
            query = query.Where(p => p.Ativo);
        }
        if (tipoProdutoId is not null)
        {
            query = query.Where(p => p.TipoProdutoId == tipoProdutoId);
        }
        return await query.OrderBy(p => p.Nome).ToListAsync();
    }

    public async Task<Produto?> ObterPorIdAsync(int id)
        => await db.Produtos.Include(p => p.TipoProduto).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Produto> CriarAsync(Produto produto)
    {
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();
        return produto;
    }

    public async Task AtualizarAsync(Produto produtoAtualizado)
    {
        var produto = await db.Produtos.FirstAsync(p => p.Id == produtoAtualizado.Id);
        produto.Nome = produtoAtualizado.Nome;
        produto.TipoProdutoId = produtoAtualizado.TipoProdutoId;
        produto.ArtistaGrupo = produtoAtualizado.ArtistaGrupo;
        produto.Preco = produtoAtualizado.Preco;
        produto.QuantidadeEstoque = produtoAtualizado.QuantidadeEstoque;
        await db.SaveChangesAsync();
    }

    public async Task DesativarAsync(int id)
    {
        var produto = await db.Produtos.FirstAsync(p => p.Id == id);
        produto.Ativo = false;
        await db.SaveChangesAsync();
    }

    public async Task ReativarAsync(int id)
    {
        var produto = await db.Produtos.FirstAsync(p => p.Id == id);
        produto.Ativo = true;
        await db.SaveChangesAsync();
    }
}
