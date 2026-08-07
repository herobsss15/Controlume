namespace Controlume.Web.Domain;

public enum Role
{
    /// <summary>Acesso total, incluindo cadastros de produto e tipo de produto (regra 17).</summary>
    Admin,

    /// <summary>Venda, caixa, sangria e históricos — sem acesso aos cadastros (regra 17).</summary>
    Operador,

    /// <summary>Vê todas as telas, mas nenhuma ação de escrita (regra 18).</summary>
    Stakeholder
}
