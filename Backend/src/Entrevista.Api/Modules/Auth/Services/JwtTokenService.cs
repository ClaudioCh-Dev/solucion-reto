using System.Security.Claims;
using System.Text;
using Entrevista.Api.Modules.Auth.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Entrevista.Api.Modules.Auth.Services;

public record AccessToken(string Token, DateTime ExpiraEn);

// Genera el access token: JWT firmado con HMAC-SHA256, de vida corta. No se revoca: vale hasta que vence.
public class JwtTokenService(JwtOptions options)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Generar(Usuario usuario)
    {
        var expiraEn = DateTime.UtcNow.AddMinutes(options.AccessTokenMinutos);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer, // Emisor del token
            Audience = options.Audience, // Audiencia del token
            Expires = expiraEn, // Fecha de expiración
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                SecurityAlgorithms.HmacSha256) // Algoritmo de firma
        });

        return new AccessToken(token, expiraEn);
    }
}
