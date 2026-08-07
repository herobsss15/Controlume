using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class VendaPagamentoConfiguration : IEntityTypeConfiguration<VendaPagamento>
{
    public void Configure(EntityTypeBuilder<VendaPagamento> builder)
    {
        builder.Property(p => p.Valor).HasPrecision(10, 2);

        builder.HasOne(p => p.Venda)
            .WithMany(v => v.Pagamentos)
            .HasForeignKey(p => p.VendaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Regra 24: excluir forma de pagamento referenciada é recusado no service, e o Restrict
        // aqui é a mesma trava no nível do banco.
        builder.HasOne(p => p.FormaPagamento)
            .WithMany(f => f.Pagamentos)
            .HasForeignKey(p => p.FormaPagamentoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
