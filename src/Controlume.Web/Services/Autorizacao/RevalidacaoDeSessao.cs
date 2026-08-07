using System.Security.Claims;
using Controlume.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;

namespace Controlume.Web.Services.Autorizacao;

/// <summary>
/// O papel e o status do usuário viajam no cookie, gravados no login. Como agora eles mudam
/// em runtime (tela de usuários), a sessão precisa ser reconferida contra o banco: sem isso,
/// desativar alguém só teria efeito quando o cookie dele expirasse, dias depois.
/// Esta classe cobre o circuito Blazor já aberto; o evento do cookie (Program.cs) cobre os
/// carregamentos de página.
/// </summary>
public class RevalidacaoDeSessao(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        // O circuito é de longa duração e não tem o escopo da requisição, então abre o seu.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlumeDbContext>();
        return await ContinuaValidaAsync(db, authenticationState.User);
    }

    /// <summary>Usuário ainda existe, continua ativo e o papel do cookie ainda bate com o do banco.</summary>
    public static async Task<bool> ContinuaValidaAsync(ControlumeDbContext db, ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            return false;
        }

        var role = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id && u.Ativo)
            .Select(u => u.Role.ToString())
            .FirstOrDefaultAsync();

        return role is not null && role == principal.FindFirstValue(ClaimTypes.Role);
    }
}
