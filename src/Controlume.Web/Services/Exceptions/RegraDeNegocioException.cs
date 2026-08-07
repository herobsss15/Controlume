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

public class DadosDoUsuarioIncompletosException : RegraDeNegocioException
{
    public DadosDoUsuarioIncompletosException()
        : base("Nome e login são obrigatórios.")
    {
    }
}

public class LoginJaExisteException : RegraDeNegocioException
{
    public LoginJaExisteException(string login)
        : base($"Já existe um usuário com o login \"{login}\".")
    {
    }
}

public class SenhaCurtaException : RegraDeNegocioException
{
    public SenhaCurtaException(int minimo)
        : base($"A senha precisa ter pelo menos {minimo} caracteres.")
    {
    }
}

/// <summary>Sem nenhum Admin ativo ninguém mais administra o sistema — nem para desfazer a mudança.</summary>
public class UltimoAdminException : RegraDeNegocioException
{
    public UltimoAdminException()
        : base("Este é o único administrador ativo. Promova outro usuário a Admin antes de desativá-lo ou trocar o papel dele.")
    {
    }
}

public class NaoPodeDesativarPropriaContaException : RegraDeNegocioException
{
    public NaoPodeDesativarPropriaContaException()
        : base("Você não pode desativar o próprio usuário.")
    {
    }
}

/// <summary>Regra 24: mesmo padrão da regra 8 para TipoProduto — desativa, não exclui.</summary>
public class FormaPagamentoReferenciadaException : RegraDeNegocioException
{
    public FormaPagamentoReferenciadaException(string nome)
        : base($"A forma de pagamento \"{nome}\" não pode ser excluída porque há vendas registradas com ela. Desative-a em vez de excluir.")
    {
    }
}

public class FormaPagamentoInvalidaException : RegraDeNegocioException
{
    public FormaPagamentoInvalidaException()
        : base("Selecione uma forma de pagamento ativa para cada pagamento da venda.")
    {
    }
}

/// <summary>Regra 22: a confirmação de recebimento é de mão única.</summary>
public class PagamentoJaRecebidoException : RegraDeNegocioException
{
    public PagamentoJaRecebidoException()
        : base("Este pagamento já está marcado como recebido, e essa marcação não pode ser desfeita.")
    {
    }
}

public class MotivoCancelamentoObrigatorioException : RegraDeNegocioException
{
    public MotivoCancelamentoObrigatorioException()
        : base("Informe o motivo do cancelamento da venda.")
    {
    }
}

public class VendaJaCanceladaException : RegraDeNegocioException
{
    public VendaJaCanceladaException(int vendaId)
        : base($"A venda #{vendaId} já está cancelada. O cancelamento é definitivo: para corrigir, registre uma venda nova.")
    {
    }
}

/// <summary>Regra 29: caixa já fechado é território de Admin.</summary>
public class CancelamentoDeCaixaFechadoException : RegraDeNegocioException
{
    public CancelamentoDeCaixaFechadoException()
        : base("Esta venda pertence a um caixa já fechado: só um administrador pode cancelá-la.")
    {
    }
}
