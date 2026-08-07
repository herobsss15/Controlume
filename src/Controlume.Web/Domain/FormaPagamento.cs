namespace Controlume.Web.Domain;

/// <summary>
/// Forma de pagamento em tabela, e não enum: o conjunto cresce quando entra um canal de venda
/// novo (Mercado Livre e afins), e o cadastro precisa acompanhar sem release — mesmo raciocínio
/// já aplicado a <see cref="TipoProduto"/>.
/// </summary>
public class FormaPagamento
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Regra 23: true só para o que ocupa a gaveta fisicamente. É esta flag — e não o nome da
    /// forma — que decide o que entra em SaldoFinal e no limite da sangria.
    /// </summary>
    public bool ContaComoCaixaFisico { get; set; }

    /// <summary>
    /// Regra 21: true para formas com repasse posterior (o dinheiro do Mercado Livre não cai no
    /// mesmo dia). Decide se o pagamento nasce Recebido ou aguardando confirmação.
    /// </summary>
    public bool RequerConfirmacaoRecebimento { get; set; }

    public ICollection<VendaPagamento> Pagamentos { get; set; } = [];
}
