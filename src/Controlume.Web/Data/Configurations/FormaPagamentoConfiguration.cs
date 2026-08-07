using Controlume.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Controlume.Web.Data.Configurations;

public class FormaPagamentoConfiguration : IEntityTypeConfiguration<FormaPagamento>
{
    /// <summary>
    /// Ids fixos porque a migration que converte VendaPagamento.TipoPagamento (string) em FK
    /// precisa citá-los no UPDATE de backfill, e os testes escolhem a forma por id.
    /// </summary>
    public const int DinheiroId = 1;
    public const int CartaoId = 2;
    public const int PixId = 3;
    public const int MercadoLivreId = 4;

    public void Configure(EntityTypeBuilder<FormaPagamento> builder)
    {
        builder.Property(f => f.Nome).HasMaxLength(100).IsRequired();

        builder.HasIndex(f => f.Nome).IsUnique();

        // Dinheiro é hoje a única forma que ocupa a gaveta (regra 23) e o Mercado Livre a única
        // com repasse posterior (regra 21) — mas isso é dado, não código: o cadastro pode mudar.
        builder.HasData(
            new FormaPagamento { Id = DinheiroId, Nome = "Dinheiro", Ativo = true, ContaComoCaixaFisico = true, RequerConfirmacaoRecebimento = false },
            new FormaPagamento { Id = CartaoId, Nome = "Cartão", Ativo = true, ContaComoCaixaFisico = false, RequerConfirmacaoRecebimento = false },
            new FormaPagamento { Id = PixId, Nome = "Pix", Ativo = true, ContaComoCaixaFisico = false, RequerConfirmacaoRecebimento = false },
            new FormaPagamento { Id = MercadoLivreId, Nome = "Mercado Livre", Ativo = true, ContaComoCaixaFisico = false, RequerConfirmacaoRecebimento = true }
        );
    }
}
