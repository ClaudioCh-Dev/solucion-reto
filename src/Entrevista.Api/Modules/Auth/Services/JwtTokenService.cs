using System.Security.Claims;
using System.Text;
using Entrevista.Api.Modules.Auth.DTOs;
using Entrevista.Api.Modules.Auth.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Entrevista.Api.Modules.Auth.Services;

// Genera el JWT firmado con HMAC-SHA256. No hay revocación: el token vale hasta que vence.
public class JwtTokenService(JwtOptions options)
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenDto Generar(Usuario usuario)
    {
        var expiraEn = DateTime.UtcNow.AddMinutes(options.ExpiracionMinutos);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiraEn,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256)
        });

        return new TokenDto(token, expiraEn);
    }
}
