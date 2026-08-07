using Controlume.Web.Domain;
using Controlume.Web.Services.Exceptions;

namespace Controlume.Web.Services.Autorizacao;

/// <summary>
/// Quem está logado, do ponto de vista dos services. Existe para que as regras 17 e 18
/// sejam aplicadas na camada de serviço, e não só escondendo botões na UI.
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>Papel do usuário autenticado, ou <c>null</c> quando não há usuário (fail closed).</summary>
    Task<Role?> ObterRoleAsync();
}

public static class UsuarioAtualExtensions
{
    /// <summary>Regra 18: Stakeholder (e anônimo) não executa nenhuma escrita.</summary>
    public static async Task<bool> PodeEscreverAsync(this IUsuarioAtual usuarioAtual)
        => await usuarioAtual.ObterRoleAsync() is Role.Admin or Role.Operador;

    /// <summary>Regra 17: cadastro/edição/desativação de produto e tipo de produto só para Admin.</summary>
    public static async Task<bool> EhAdminAsync(this IUsuarioAtual usuarioAtual)
        => await usuarioAtual.ObterRoleAsync() is Role.Admin;

    public static async Task GarantirPodeEscreverAsync(this IUsuarioAtual usuarioAtual)
    {
        if (!await usuarioAtual.PodeEscreverAsync())
        {
            throw new AcessoNegadoException();
        }
    }

    public static async Task GarantirAdminAsync(this IUsuarioAtual usuarioAtual)
    {
        if (!await usuarioAtual.EhAdminAsync())
        {
            throw new AcessoNegadoException();
        }
    }
}
