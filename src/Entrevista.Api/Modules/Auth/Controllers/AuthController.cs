using System.Security.Claims;
using Entrevista.Api.Modules.Auth.DTOs;
using Entrevista.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Entrevista.Api.Modules.Auth.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    // POST api/auth/login
    // No hay endpoint de logout: el JWT no se revoca. El cliente borra el token y este sigue válido hasta vencer.
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var token = await auth.LoginAsync(dto, ct);
        if (token is null)
            // Mismo mensaje si el email no existe o la contraseña es incorrecta: no revela qué usuarios hay.
            return Problem(title: "Credenciales inválidas.", statusCode: StatusCodes.Status401Unauthorized);

        return token;
    }

    // GET api/auth/me
    // Datos del usuario del token. Sirve al cliente para comprobar si la sesión sigue activa.
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UsuarioDto> Me() => new UsuarioDto(
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!),
        User.FindFirstValue(JwtRegisteredClaimNames.Email)!);
}
