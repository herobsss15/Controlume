using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class ItemVendaConfiguration : IEntityTypeConfiguration<ItemVenda>
{
    public void Configure(EntityTypeBuilder<ItemVenda> builder)
    {
        builder.Property(i => i.PrecoTabela).HasPrecision(10, 2);
        builder.Property(i => i.PrecoVenda).HasPrecision(10, 2);

        builder.HasOne(i => i.Venda)
            .WithMany(v => v.Itens)
            .HasForeignKey(i => i.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
