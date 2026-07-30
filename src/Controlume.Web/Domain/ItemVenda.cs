namespace Controlume.Web.Domain;

public class ItemVenda
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public Venda? Venda { get; set; }
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public int Quantidade { get; set; }

    // Cópia de Produto.Preco no instante da venda. Congelado — nunca recalculado depois.
    public decimal PrecoTabela { get; set; }

    // Valor efetivamente cobrado; pode divergir de PrecoTabela (negociação/desconto).
    public decimal PrecoVenda { get; set; }
}
