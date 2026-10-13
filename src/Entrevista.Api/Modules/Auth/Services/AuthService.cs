using Entrevista.Api.Data;
using Entrevista.Api.Modules.Auth.DTOs;
using Entrevista.Api.Modules.Auth.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Auth.Services;

// Las contraseñas se guardan con PasswordHasher de ASP.NET Core Identity:
// PBKDF2 (HMAC-SHA512, 100.000 iteraciones) con sal aleatoria incluida en el propio hash.
public class AuthService(
    AppDbContext db,
    IPasswordHasher<Usuario> hasher,
    JwtTokenService tokens,
    ILogger<AuthService> logger)
{
    // Devuelve null si el email no existe o la contraseña no coincide (el controlador no distingue ambos casos).
    public async Task<TokenDto?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = NormalizarEmail(dto.Email);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (usuario is null)
        {
            // Se hashea igual para que la respuesta tarde lo mismo exista o no el email.
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
            await db.SaveChangesAsync(ct);
        }

        return tokens.Generar(usuario);
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

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
