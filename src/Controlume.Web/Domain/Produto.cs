namespace Controlume.Web.Domain;

public class Produto
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public int TipoProdutoId { get; set; }
    public TipoProduto? TipoProduto { get; set; }

    // Só preenchido para discos/CDs/DVDs; nulo para eletrônicos e avulsos. Nunca obrigatório.
    public string? ArtistaGrupo { get; set; }

    public decimal Preco { get; set; }
    public int QuantidadeEstoque { get; set; }
    public bool Ativo { get; set; } = true;
}
