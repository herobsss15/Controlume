namespace Controlume.Web.Domain;

public class VendaPagamento
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public Venda? Venda { get; set; }
    public int FormaPagamentoId { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }
    public decimal Valor { get; set; }

    // Regras 21 e 22: nasce conforme FormaPagamento.RequerConfirmacaoRecebimento e só anda
    // para frente — não existe caminho, aqui ou no service, que devolva isto para false.
    public bool Recebido { get; set; }
    public DateTime? DataRecebimento { get; set; }
}
