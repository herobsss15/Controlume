namespace Controlume.Web.Domain;

public class TipoProduto
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public bool Ativo { get; set; } = true;

    public ICollection<Produto> Produtos { get; set; } = [];
}
