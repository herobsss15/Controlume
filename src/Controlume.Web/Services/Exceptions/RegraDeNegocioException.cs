namespace Controlume.Web.Services.Exceptions;

/// <summary>Base para violações de regra de negócio que a UI deve mostrar como mensagem amigável.</summary>
public abstract class RegraDeNegocioException : Exception
{
    protected RegraDeNegocioException(string message) : base(message)
    {
    }
}

public class TipoProdutoReferenciadoException : RegraDeNegocioException
{
    public TipoProdutoReferenciadoException(string nome)
        : base($"O tipo \"{nome}\" não pode ser excluído porque há produtos cadastrados com esse tipo. Desative-o em vez de excluir.")
    {
    }
}

public class CaixaJaAbertoException : RegraDeNegocioException
{
    public CaixaJaAbertoException()
        : base("Já existe um caixa aberto. Feche o caixa atual antes de abrir um novo.")
    {
    }
}

public class NenhumCaixaAbertoException : RegraDeNegocioException
{
    public NenhumCaixaAbertoException()
        : base("Não há caixa aberto. Abra um caixa antes de registrar uma venda.")
    {
    }
}

public class PagamentoDivergenteException : RegraDeNegocioException
{
    public PagamentoDivergenteException(decimal totalVenda, decimal totalPagamentos)
        : base($"A soma dos pagamentos (R$ {totalPagamentos:0.00}) não corresponde ao total da venda (R$ {totalVenda:0.00}).")
    {
    }
}

public class EstoqueInsuficienteException : RegraDeNegocioException
{
    public EstoqueInsuficienteException(IReadOnlyList<string> produtos)
        : base($"Estoque insuficiente para: {string.Join(", ", produtos)}.")
    {
    }
}

public class VendaSemItensException : RegraDeNegocioException
{
    public VendaSemItensException()
        : base("A venda precisa ter pelo menos um item.")
    {
    }
}

/// <summary>Regras 17 e 18: a checagem de papel também vale na camada de serviço, não só na UI.</summary>
public class AcessoNegadoException : RegraDeNegocioException
{
    public AcessoNegadoException()
        : base("Seu usuário não tem permissão para executar esta ação.")
    {
    }
}

public class SangriaSemMotivoException : RegraDeNegocioException
{
    public SangriaSemMotivoException()
        : base("Informe o motivo da sangria (Pagamento ou Compra).")
    {
    }
}

public class ValorSangriaInvalidoException : RegraDeNegocioException
{
    public ValorSangriaInvalidoException()
        : base("O valor da sangria precisa ser maior que zero.")
    {
    }
}

public class SaldoInsuficienteParaSangriaException : RegraDeNegocioException
{
    public SaldoInsuficienteParaSangriaException(decimal valor, decimal saldoDisponivel)
        : base($"Sangria de R$ {valor:0.00} deixaria o caixa negativo: há apenas R$ {saldoDisponivel:0.00} em dinheiro na gaveta.")
    {
    }
}
