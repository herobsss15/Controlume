namespace Controlume.Web.Domain;

public class Venda
{
    public int Id { get; set; }
    public int FechamentoCaixaId { get; set; }
    public FechamentoCaixa? FechamentoCaixa { get; set; }
    public DateTime DataHora { get; set; }

    // Soma de ItemVenda.PrecoVenda * Quantidade, calculada no servidor no momento da confirmação.
    public decimal ValorTotal { get; set; }

    // Regra 26: cancelamento é delete lógico — a venda continua no histórico, marcada e com o
    // motivo à vista. Regra 28: uma vez cancelada, fica fora de todo cálculo financeiro.
    public bool Cancelada { get; set; }
    public string? MotivoCancelamento { get; set; }
    public DateTime? DataCancelamento { get; set; }
    public int? CanceladoPorUsuarioId { get; set; }
    public Usuario? CanceladoPorUsuario { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = [];
    public ICollection<VendaPagamento> Pagamentos { get; set; } = [];
}
