using Controlume.Web.Domain;
using Controlume.Web.Services.Autorizacao;

namespace Controlume.Tests;

/// <summary>Papel fixo, para o teste escolher quem está "logado" sem cookie nem circuito Blazor.</summary>
public class UsuarioAtualFake(Role? role, int? id = null) : IUsuarioAtual
{
    public Task<Role?> ObterRoleAsync() => Task.FromResult(role);

    public Task<int?> ObterIdAsync() => Task.FromResult(id);
}
