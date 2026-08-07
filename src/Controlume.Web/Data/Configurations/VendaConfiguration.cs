using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class VendaConfiguration : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.Property(v => v.ValorTotal).HasPrecision(10, 2);

        builder.Property(v => v.MotivoCancelamento).HasMaxLength(300);

        builder.HasOne(v => v.FechamentoCaixa)
            .WithMany(c => c.Vendas)
            .HasForeignKey(v => v.FechamentoCaixaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Auditoria mínima de quem cancelou (regra 26). Restrict porque o usuário nunca é
        // excluído de verdade — a tela de usuários só desativa.
        builder.HasOne(v => v.CanceladoPorUsuario)
            .WithMany()
            .HasForeignKey(v => v.CanceladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
