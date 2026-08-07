using Controlume.Web.Services.Autorizacao;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Controlume.Tests;

/// <summary>
/// Trocar o AuthenticationStateProvider padrão por RevalidacaoDeSessao só funciona porque o
/// framework faz cast do provider registrado para IHostEnvironmentAuthenticationStateProvider
/// (ele não resolve essa interface pelo DI) — é por esse cast que o usuário do cookie entra no
/// circuito. Se a herança mudar, nada quebra no build: o app passa a enxergar todo mundo como
/// anônimo, os botões de Admin somem e os services recusam escrita. Daí o teste.
/// </summary>
public class AuthenticationStateProviderWiringTests
{
    [Fact]
    public void RevalidacaoDeSessao_EhOProviderRegistradoEAceitaOUsuarioVindoDoCookie()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRazorComponents().AddInteractiveServerComponents();
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, RevalidacaoDeSessao>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authenticationStateProvider = scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();

        Assert.IsType<RevalidacaoDeSessao>(authenticationStateProvider);
        Assert.IsAssignableFrom<IHostEnvironmentAuthenticationStateProvider>(authenticationStateProvider);
    }
}
