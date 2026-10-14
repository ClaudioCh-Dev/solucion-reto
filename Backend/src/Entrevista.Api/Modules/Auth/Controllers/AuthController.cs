using System.Security.Claims;
using Entrevista.Api.Common.Errores;
using Entrevista.Api.Modules.Auth.DTOs;
using Entrevista.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Entrevista.Api.Modules.Auth.Controllers;

// Access token (JWT, vida corta) en el body: el cliente lo guarda en memoria y lo envía como Bearer.
// Refresh token en cookie (configurable en Jwt:RefreshCookie): solo se envía a /api/auth.
[ApiController]
[Route("auth")]
public class AuthController(AuthService auth, JwtOptions options) : ControllerBase
{
    private RefreshCookieOptions Cookie => options.RefreshCookie;

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SesionDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var emitida = await auth.LoginAsync(dto, ct);
        if (emitida is null)
            // Mismo mensaje si el email no existe o la contraseña es incorrecta: no revela qué usuarios hay.
            return this.Error(ErroresApi.CredencialesInvalidas);

        EscribirCookie(emitida.RefreshToken, emitida.RefreshExpiraEn);
        return emitida.Sesion;
    }

    // POST api/auth/refresh
    // Usa la cookie del refresh token, la rota y devuelve un access token nuevo.
    // El cliente lo llama al recargar la página (el access token vive solo en memoria) y antes de que venza.
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SesionDto>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[Cookie.Nombre];
        var emitida = string.IsNullOrEmpty(refreshToken) ? null : await auth.RefrescarAsync(refreshToken, ct);
        if (emitida is null)
        {
            BorrarCookie();
            return this.Error(ErroresApi.SesionExpirada);
        }

        EscribirCookie(emitida.RefreshToken, emitida.RefreshExpiraEn);
        return emitida.Sesion;
    }

    // POST api/auth/logout
    // Revoca el refresh token en la base y borra la cookie. El access token ya emitido
    // sigue siendo válido hasta que vence (como máximo Jwt:AccessTokenMinutos).
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[Cookie.Nombre];
        if (!string.IsNullOrEmpty(refreshToken))
            await auth.CerrarSesionAsync(refreshToken, ct);

        BorrarCookie();
        return NoContent();
    }

    // GET api/auth/me
    // Datos del usuario del access token.
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioDto> Me() => new UsuarioDto(
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
        User.FindFirstValue(JwtRegisteredClaimNames.Email)!);

    private void EscribirCookie(string valor, DateTime expiraEn) =>
        Response.Cookies.Append(Cookie.Nombre, valor, CrearOpciones(expiraEn));

    // Para que el navegador la borre, Path/SameSite/Secure deben coincidir con los de cuando se creó.
    private void BorrarCookie() =>
        Response.Cookies.Delete(Cookie.Nombre, CrearOpciones(expiraEn: null));

    private CookieOptions CrearOpciones(DateTime? expiraEn) => new()
    {
        HttpOnly = Cookie.HttpOnly,
        Secure = Cookie.Secure,
        SameSite = Cookie.SameSite,
        Path = Cookie.Path,
        Expires = expiraEn,
        IsEssential = true
    };
}
