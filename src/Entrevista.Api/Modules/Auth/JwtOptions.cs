namespace Entrevista.Api.Modules.Auth;

// Sección "Jwt" de appsettings. Se valida al arrancar en Program.cs.
public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;          // HMAC-SHA256: mínimo 32 bytes
    public int ExpiracionMinutos { get; set; } = 60;
}
