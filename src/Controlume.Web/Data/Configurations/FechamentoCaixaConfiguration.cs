using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class FechamentoCaixaConfiguration : IEntityTypeConfiguration<FechamentoCaixa>
{
    public void Configure(EntityTypeBuilder<FechamentoCaixa> builder)
    {
        builder.Property(c => c.ValorInicial).HasPrecision(10, 2);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Regra 2: só um caixa aberto por vez. Defesa em profundidade no nível do banco,
        // além da checagem em CaixaService.AbrirCaixaAsync. O literal 'Aberto' aqui precisa
        // bater com StatusCaixa.Aberto.ToString() e com o nome de coluna snake_case ("status").
        builder.HasIndex(c => c.Status)
            .IsUnique()
            .HasFilter("status = 'Aberto'");
    }
}
