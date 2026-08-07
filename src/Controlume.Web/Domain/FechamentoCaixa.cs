namespace Controlume.Web.Domain;

public class FechamentoCaixa
{
    public int Id { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public decimal ValorInicial { get; set; }
    public StatusCaixa Status { get; set; } = StatusCaixa.Aberto;

    public ICollection<Venda> Vendas { get; set; } = [];
}
