using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class VendaPagamentoConfiguration : IEntityTypeConfiguration<VendaPagamento>
{
    public void Configure(EntityTypeBuilder<VendaPagamento> builder)
    {
        builder.Property(p => p.Valor).HasPrecision(10, 2);

        builder.Property(p => p.TipoPagamento)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(p => p.Venda)
            .WithMany(v => v.Pagamentos)
            .HasForeignKey(p => p.VendaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
