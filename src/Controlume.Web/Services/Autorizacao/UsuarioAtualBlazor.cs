using System.Security.Claims;
using Controlume.Web.Domain;
using Microsoft.AspNetCore.Components.Authorization;

namespace Controlume.Web.Services.Autorizacao;

/// <summary>
/// Lê o papel do cookie de autenticação pelo <see cref="AuthenticationStateProvider"/>, que
/// funciona tanto no render estático quanto dentro do circuito Blazor Server — ao contrário
/// de HttpContext, que não existe mais depois que o circuito é estabelecido.
/// </summary>
public class UsuarioAtualBlazor(AuthenticationStateProvider authenticationStateProvider) : IUsuarioAtual
{
    public async Task<Role?> ObterRoleAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return Enum.TryParse<Role>(state.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
    }
}
