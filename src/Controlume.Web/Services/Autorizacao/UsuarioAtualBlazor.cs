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
        var user = await ObterUsuarioAsync();
        return Enum.TryParse<Role>(user?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
    }

    public async Task<int?> ObterIdAsync()
    {
        var user = await ObterUsuarioAsync();
        return int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    private async Task<ClaimsPrincipal?> ObterUsuarioAsync()
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true ? state.User : null;
    }
}
