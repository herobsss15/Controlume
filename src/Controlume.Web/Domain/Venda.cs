namespace Controlume.Web.Domain;

public class Venda
{
    public int Id { get; set; }
    public int FechamentoCaixaId { get; set; }
    public FechamentoCaixa? FechamentoCaixa { get; set; }
    public DateTime DataHora { get; set; }

    // Soma de ItemVenda.PrecoVenda * Quantidade, calculada no servidor no momento da confirmação.
    public decimal ValorTotal { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = [];
    public ICollection<VendaPagamento> Pagamentos { get; set; } = [];
}
