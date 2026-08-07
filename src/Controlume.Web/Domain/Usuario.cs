namespace Controlume.Web.Domain;

public class Usuario
{
    public int Id { get; set; }
    public required string Nome { get; set; }

    // Normalizado em minúsculas (ver UsuarioService) para que o índice único e a
    // comparação no login não dependam de como o usuário digitou.
    public required string Login { get; set; }

    public required string SenhaHash { get; set; }
    public Role Role { get; set; }
    public bool Ativo { get; set; } = true;
}
