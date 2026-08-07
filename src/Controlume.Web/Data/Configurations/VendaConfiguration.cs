using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.Property(v => v.ValorTotal).HasPrecision(10, 2);

        builder.HasOne(v => v.FechamentoCaixa)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.FechamentoCaixaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
