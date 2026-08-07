namespace Controlume.Web.Domain;

/// <summary>Retirada de dinheiro da gaveta, sempre vinculada a um caixa aberto (regra 11).</summary>
public class Sangria
{
    public int Id { get; set; }
    public int FechamentoCaixaId { get; set; }
    public FechamentoCaixa? FechamentoCaixa { get; set; }
    public DateTime DataHora { get; set; }

    // Sempre positivo: a subtração acontece no cálculo do saldo, não no sinal do valor.
    public decimal Valor { get; set; }

    public MotivoSangria Motivo { get; set; }

    // Complemento livre do motivo, ex: "pagamento fornecedor X". Nunca obrigatório.
    public string? Descricao { get; set; }
}
