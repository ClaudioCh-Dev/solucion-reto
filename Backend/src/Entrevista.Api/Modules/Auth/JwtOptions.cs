namespace Entrevista.Api.Modules.Auth;

// Sección "Jwt" de appsettings. Se valida al arrancar en Program.cs.
public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;          // HMAC-SHA256: mínimo 32 bytes

    public int AccessTokenMinutos { get; set; } = 15;
    public int RefreshTokenDias { get; set; } = 7;

    public RefreshCookieOptions RefreshCookie { get; set; } = new();
}

// Sección "Jwt:RefreshCookie": cómo se envía el refresh token. Cada perfil puede cambiarla.
// Los valores por defecto son los seguros; en dev solo se desactiva Secure porque se usa HTTP.
public class RefreshCookieOptions
{
    public string Nombre { get; set; } = "refresh_token";

    // Solo se envía a /api/auth (refresh y logout), no en cada petición a la API.
    public string Path { get; set; } = "/api/auth";

    // true: JavaScript no puede leer la cookie (protege el refresh token ante XSS).
    public bool HttpOnly { get; set; } = true;

    // Strict: el navegador no la envía desde otros sitios (protege ante CSRF). Valores: Strict, Lax, None.
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    // true: solo viaja por HTTPS. SameSite=None exige Secure=true.
    public bool Secure { get; set; } = true;
}
