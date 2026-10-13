using System.Security.Cryptography;
using Entrevista.Api.Data;
using Entrevista.Api.Modules.Auth.DTOs;
using Entrevista.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Auth.Services;

// Sesión emitida: el DTO para el body (con el access token) y el refresh token para la cookie.
public record SesionEmitida(SesionDto Sesion, string RefreshToken, DateTime RefreshExpiraEn);

// Las contraseñas se guardan con PasswordHasher de ASP.NET Core Identity:
// PBKDF2 (HMAC-SHA512, 100.000 iteraciones) con sal aleatoria incluida en el propio hash.
//
// Refresh tokens: valor aleatorio de 64 bytes; en la base solo se guarda su hash SHA-256.
// Cada refresh lo rota (revoca el actual y emite uno nuevo). Si llega un token ya rotado,
// se asume robo y se revocan todos los refresh tokens del usuario.
public class AuthService(
    AppDbContext db,
    IPasswordHasher<Usuario> hasher,
    JwtTokenService tokens,
    JwtOptions options,
    ILogger<AuthService> logger)
{
    // Devuelve null si el email no existe o la contraseña no coincide (el controlador no distingue ambos casos).
    public async Task<SesionEmitida?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = NormalizarEmail(dto.Email);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null)
        {
            // Se hashea igual para que la respuesta tarde lo mismo exista o no el email. Para evitar timing attacks.
            hasher.HashPassword(new Usuario(), dto.Password);
            return null;
        }

        var resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, dto.Password);
        if (resultado == PasswordVerificationResult.Failed)
            return null;

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // El hash se creó con parámetros más débiles que los actuales: se actualiza.
            usuario.PasswordHash = hasher.HashPassword(usuario, dto.Password);
        }

        var sesion = EmitirSesion(usuario);
        await db.SaveChangesAsync(ct);
        return sesion;
    }

    // Rota el refresh token. Devuelve null si no existe, venció, fue revocado o se detectó reúso.
    public async Task<SesionEmitida?> RefrescarAsync(string refreshToken, CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var hash = Hash(refreshToken);
        var actual = await db.RefreshTokens
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (actual is null)
            return null;

        if (actual.RevocadoEn is not null)
        {
            if (actual.ReemplazadoPorHash is not null)
            {
                // Un token ya rotado se volvió a usar: alguien más lo tiene. Se cierran todas las sesiones.
                logger.LogWarning("Reúso de refresh token del usuario {UsuarioId}. Se revocan todas sus sesiones.", actual.UsuarioId);
                await RevocarTodosAsync(actual.UsuarioId, ahora, ct);
            }
            return null;
        }

        if (actual.ExpiraEn <= ahora)
            return null;

        var sesion = EmitirSesion(actual.Usuario);
        actual.RevocadoEn = ahora;
        actual.ReemplazadoPorHash = Hash(sesion.RefreshToken);
        await db.SaveChangesAsync(ct);
        return sesion;
    }

    // Revoca el refresh token (logout). El access token ya emitido sigue válido hasta vencer.
    public async Task CerrarSesionAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var actual = await db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (actual is null || actual.RevocadoEn is not null)
            return;

        actual.RevocadoEn = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // Crea el usuario si no existe. Se usa al arrancar en dev para el usuario de demostración.
    public async Task CrearUsuarioSiNoExisteAsync(string email, string password, CancellationToken ct = default)
    {
        email = NormalizarEmail(email);
        if (await db.Usuarios.AnyAsync(u => u.Email == email, ct))
            return;

        var usuario = new Usuario { Email = email };
        usuario.PasswordHash = hasher.HashPassword(usuario, password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Usuario {Email} creado.", email);
    }

    // Genera access token + refresh token y agrega el refresh al contexto (lo guarda quien llama).
    private SesionEmitida EmitirSesion(Usuario usuario)
    {
        var access = tokens.Generar(usuario);

        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var ahora = DateTime.UtcNow;
        var refreshExpiraEn = ahora.AddDays(options.RefreshTokenDias);

        db.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = Hash(refresh),
            CreadoEn = ahora,
            ExpiraEn = refreshExpiraEn,
            Usuario = usuario
        });

        var sesion = new SesionDto(access.Token, access.ExpiraEn, new UsuarioDto(usuario.Id, usuario.Email));
        return new SesionEmitida(sesion, refresh, refreshExpiraEn);
    }

    private async Task RevocarTodosAsync(int usuarioId, DateTime ahora, CancellationToken ct)
    {
        await db.RefreshTokens
            .Where(r => r.UsuarioId == usuarioId && r.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevocadoEn, ahora), ct);
    }

    private static string Hash(string valor) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(valor)));

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
