using Controlume.Web.Data.Configurations;

namespace Controlume.Tests;

/// <summary>
/// Atalho para as formas de pagamento semeadas por HasData e criadas no banco de teste pelo
/// EnsureCreated. Aponta para as constantes de produção para os ids não poderem divergir.
/// Dinheiro é a única que conta como caixa físico; Mercado Livre, a única com repasse posterior.
/// </summary>
public static class Formas
{
    public const int Dinheiro = FormaPagamentoConfiguration.DinheiroId;
    public const int Cartao = FormaPagamentoConfiguration.CartaoId;
    public const int Pix = FormaPagamentoConfiguration.PixId;
    public const int MercadoLivre = FormaPagamentoConfiguration.MercadoLivreId;
}
