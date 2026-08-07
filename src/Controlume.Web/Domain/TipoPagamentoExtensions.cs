namespace Controlume.Web.Domain;

public static class TipoPagamentoExtensions
{
    public static string ParaExibicao(this TipoPagamento tipo) => tipo switch
    {
        TipoPagamento.Cartao => "Cartão",
        TipoPagamento.Pix => "Pix",
        TipoPagamento.Dinheiro => "Dinheiro",
        _ => tipo.ToString()
    };
}
