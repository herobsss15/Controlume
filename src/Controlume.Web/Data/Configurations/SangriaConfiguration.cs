using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class SangriaConfiguration : IEntityTypeConfiguration<Sangria>
{
    public void Configure(EntityTypeBuilder<Sangria> builder)
    {
        builder.Property(s => s.Valor).HasPrecision(10, 2);

        builder.Property(s => s.Motivo)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Descricao).HasMaxLength(200);

        builder.HasOne(s => s.FechamentoCaixa)
            .WithMany(c => c.Sangrias)
            .HasForeignKey(s => s.FechamentoCaixaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
