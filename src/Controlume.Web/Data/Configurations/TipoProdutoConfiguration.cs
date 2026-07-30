using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class TipoProdutoConfiguration : IEntityTypeConfiguration<TipoProduto>
{
    public void Configure(EntityTypeBuilder<TipoProduto> builder)
    {
        builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();

        builder.HasData(
            new TipoProduto { Id = 1, Nome = "Disco", Ativo = true },
            new TipoProduto { Id = 2, Nome = "CD", Ativo = true },
            new TipoProduto { Id = 3, Nome = "DVD", Ativo = true },
            new TipoProduto { Id = 4, Nome = "Eletrônico", Ativo = true },
            new TipoProduto { Id = 5, Nome = "Avulso", Ativo = true }
        );
    }
}
