namespace Entrevista.Api.Modules.Auth.Entities;

// Refresh token emitido a un usuario. Solo se guarda el hash SHA-256: el valor real viaja
// únicamente en la cookie HttpOnly del cliente.
public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; }
    public DateTime ExpiraEn { get; set; }

    // Se completa al rotarlo (refresh) o al cerrar sesión.
    public DateTime? RevocadoEn { get; set; }

    // Hash del token que lo reemplazó al rotar. Si un token revocado se vuelve a usar, es reúso (posible robo).
    public string? ReemplazadoPorHash { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public bool EstaActivo(DateTime ahora) => RevocadoEn is null && ExpiraEn > ahora;
}
