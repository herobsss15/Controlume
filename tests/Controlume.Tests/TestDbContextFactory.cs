using Controlume.Web.Data;
using Controlume.Web.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Tests;

/// <summary>
/// SQLite em memória com o mesmo modelo do app (via EnsureCreated, não pelas migrations
/// do Npgsql). Mais fiel que o provider InMemory: reforça FKs e índices únicos de verdade.
/// </summary>
public class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public ControlumeDbContext Context { get; }

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ControlumeDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        Context = new ControlumeDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>TipoProdutoId 1 ("Disco") existe sempre: vem do seed via HasData aplicado por EnsureCreated.</summary>
    public async Task<Produto> SeedProdutoAsync(decimal preco, int estoque, int? tipoProdutoId = null, string nome = "Produto de teste")
    {
        var produto = new Produto
        {
            Nome = nome,
            TipoProdutoId = tipoProdutoId ?? 1,
            Preco = preco,
            QuantidadeEstoque = estoque
        };
        Context.Produtos.Add(produto);
        await Context.SaveChangesAsync();
        return produto;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
