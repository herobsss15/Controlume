using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Data;

public class ControlumeDbContext(DbContextOptions<ControlumeDbContext> options) : DbContext(options)
{
    public DbSet<TipoProduto> TiposProduto => Set<TipoProduto>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<VendaPagamento> VendaPagamentos => Set<VendaPagamento>();
    public DbSet<FechamentoCaixa> FechamentosCaixa => Set<FechamentoCaixa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlumeDbContext).Assembly);
    }
}
