using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.Property(p => p.Nome).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ArtistaGrupo).HasMaxLength(200);
        builder.Property(p => p.Preco).HasPrecision(10, 2);

        builder.HasOne(p => p.TipoProduto)
            .WithMany(t => t.Produtos)
            .HasForeignKey(p => p.TipoProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
