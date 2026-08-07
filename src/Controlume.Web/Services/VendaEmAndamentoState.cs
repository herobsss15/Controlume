using Controlume.Web.Domain;

namespace Controlume.Web.Services;

public class ItemCarrinho(int produtoId, string nomeProduto, decimal precoTabela, int quantidade)
{
    public int ProdutoId { get; } = produtoId;
    public string NomeProduto { get; } = nomeProduto;
    public decimal PrecoTabela { get; } = precoTabela;
    public decimal PrecoVenda { get; set; } = precoTabela;
    public int Quantidade { get; set; } = quantidade;
}

/// <summary>O nome viaja junto só para a lista do carrinho não precisar reconsultar o cadastro.</summary>
public record PagamentoCarrinho(int FormaPagamentoId, string NomeFormaPagamento, decimal Valor);

/// <summary>
/// Carrinho da venda em andamento, mantido em memória por circuito Blazor Server (Scoped).
/// Não toca o banco — a persistência acontece só em VendaService.ConfirmarVendaAsync.
/// </summary>
public class VendaEmAndamentoState
{
    private readonly List<ItemCarrinho> _itens = [];
    private readonly List<PagamentoCarrinho> _pagamentos = [];

    public IReadOnlyList<ItemCarrinho> Itens => _itens;
    public IReadOnlyList<PagamentoCarrinho> Pagamentos => _pagamentos;

    public decimal ValorTotal => _itens.Sum(i => i.PrecoVenda * i.Quantidade);
    public decimal TotalPago => _pagamentos.Sum(p => p.Valor);
    public decimal Restante => ValorTotal - TotalPago;

    public void AdicionarItem(int produtoId, string nomeProduto, decimal precoTabela, int quantidade)
    {
        var existente = _itens.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (existente is not null)
        {
            existente.Quantidade += quantidade;
        }
        else
        {
            _itens.Add(new ItemCarrinho(produtoId, nomeProduto, precoTabela, quantidade));
        }
    }

    public void RemoverItem(int produtoId) => _itens.RemoveAll(i => i.ProdutoId == produtoId);

    public void AdicionarPagamento(FormaPagamento forma, decimal valor)
        => _pagamentos.Add(new PagamentoCarrinho(forma.Id, forma.Nome, valor));

    public void RemoverPagamento(int index)
    {
        if (index >= 0 && index < _pagamentos.Count)
        {
            _pagamentos.RemoveAt(index);
        }
    }

    public void Limpar()
    {
        _itens.Clear();
        _pagamentos.Clear();
    }
}
