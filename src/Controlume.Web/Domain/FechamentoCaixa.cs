namespace Controlume.Web.Domain;

public class FechamentoCaixa
{
    public int Id { get; set; }
    public DateTime DataAbertura { get; set; }
    public DateTime? DataFechamento { get; set; }
    public decimal ValorInicial { get; set; }
    public StatusCaixa Status { get; set; } = StatusCaixa.Aberto;

    // Regra 14: saldo físico em dinheiro (inicial + vendas em dinheiro − sangrias),
    // congelado no momento do fechamento. Nulo enquanto o caixa está aberto.
    public decimal? SaldoFinal { get; set; }

    public ICollection<Venda> Vendas { get; set; } = [];
    public ICollection<Sangria> Sangrias { get; set; } = [];
}
