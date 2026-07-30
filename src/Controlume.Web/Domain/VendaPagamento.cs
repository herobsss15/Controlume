namespace Controlume.Web.Domain;

public class VendaPagamento
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public Venda? Venda { get; set; }
    public TipoPagamento TipoPagamento { get; set; }
    public decimal Valor { get; set; }
}
