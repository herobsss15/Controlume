using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services;
using Microsoft.AspNetCore.Identity;
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

    // Os services exigem um IUsuarioAtual (regras 17 e 18). Admin é o padrão para que os testes
    // de regra de negócio não precisem falar de papel; quem testa autorização passa o seu, e
    // null representa requisição sem usuário autenticado.
    public CaixaService CriarCaixaService(Role? role = Role.Admin) => new(Context, new UsuarioAtualFake(role));

    public VendaService CriarVendaService(Role? role = Role.Admin) => new(Context, new UsuarioAtualFake(role));

    public ProdutoService CriarProdutoService(Role? role = Role.Admin) => new(Context, new UsuarioAtualFake(role));

    public TipoProdutoService CriarTipoProdutoService(Role? role = Role.Admin) => new(Context, new UsuarioAtualFake(role));

    public FormaPagamentoService CriarFormaPagamentoService(Role? role = Role.Admin) => new(Context, new UsuarioAtualFake(role));

    public SangriaService CriarSangriaService(Role? role = Role.Admin)
        => new(Context, CriarCaixaService(role), new UsuarioAtualFake(role));

    /// <summary><paramref name="id"/> é o usuário logado, que não pode desativar a própria conta.</summary>
    public UsuarioService CriarUsuarioService(Role? role = Role.Admin, int? id = null)
        => new(Context, new PasswordHasher<Usuario>(), new UsuarioAtualFake(role, id));

    /// <summary>Serve de autor do cancelamento quando o teste precisa checar a auditoria (regra 26).</summary>
    public async Task<Usuario> SeedUsuarioAsync(Role role = Role.Admin, string login = "operador.teste")
    {
        var usuario = new Usuario { Nome = login, Login = login, SenhaHash = "x", Role = role };
        Context.Usuarios.Add(usuario);
        await Context.SaveChangesAsync();
        return usuario;
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
