using Entrevista.Api.Common.Errores;
using Entrevista.Api.Common.OpenApi;
using Entrevista.Api.Modules.Auth.Controllers;
using Entrevista.Api.Modules.Auth.DTOs;

namespace Entrevista.Api.Modules.Auth.OpenApi;

// Textos y ejemplos de /api/auth en el documento OpenAPI (Scalar).
public class AuthDocumentacion : IDocumentacionModulo
{
    private const string TokenEjemplo =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJkZW1vQGVudHJldmlzdGEubG9jYWwifQ.firma";

    private static readonly SesionDto Sesion = new(
        TokenEjemplo, new DateTime(2026, 10, 14, 21, 33, 0, DateTimeKind.Utc), new UsuarioDto(1, "demo@entrevista.local"));

    private const string Cookie =
        "Además envía la cookie `refresh_token` (`HttpOnly`, `SameSite=Strict`, `Path=/api/auth`, 7 días). " +
        "JavaScript no puede leerla; el navegador la manda sola a `/api/auth/refresh` y `/api/auth/logout`.";

    public void Documentar(DocumentacionApi docs)
    {
        docs.Seccion("Auth", """
            Inicio y cierre de sesión con dos tokens:
            - **Access token** (JWT, 15 min): va en el cuerpo de la respuesta y se envía como `Authorization: Bearer`.
            - **Refresh token** (7 días): cookie `HttpOnly`. En la base solo se guarda su hash SHA-256 y **rota** en cada uso;
              si se reutiliza uno ya rotado, se revocan todas las sesiones del usuario.
            """);

        docs.Operacion<AuthController>(nameof(AuthController.Login))
            .Resumen("Iniciar sesión")
            .Descripcion($"""
                Valida email y contraseña y devuelve el access token.

                {Cookie}

                Si el email no existe o la contraseña es incorrecta, responde lo mismo (`401`, `code: "credenciales_invalidas"`), para no revelar qué usuarios existen.
                """)
            .Cuerpo(new LoginDto("demo@entrevista.local", "Demo#2026"))
            .Respuesta(200, "Sesión iniciada.", Sesion)
            .Respuesta(400, "Falta el email o la contraseña, o el email no tiene formato válido.",
                EjemplosComunes.Validacion("/api/auth/login",
                    ("email", "Ingresa un correo válido."),
                    ("password", "Ingresa tu contraseña.")))
            .Respuesta(401, "Email o contraseña incorrectos.", EjemplosComunes.Problema(ErroresApi.CredencialesInvalidas, "/api/auth/login"));

        docs.Operacion<AuthController>(nameof(AuthController.Refresh))
            .Resumen("Renovar el access token")
            .Descripcion("""
                Usa la cookie `refresh_token` (no lleva cuerpo ni header `Authorization`), la **rota** y devuelve un access token nuevo.

                El frontend lo llama al recargar la página (el access token vive solo en memoria) y un minuto antes de que venza.

                > Para probarlo aquí, primero ejecuta **Iniciar sesión**: el navegador guarda la cookie y la envía sola en esta petición.
                """)
            .Respuesta(200, "Token renovado. También llega una cookie `refresh_token` nueva.", Sesion)
            .Respuesta(401, "No hay cookie, venció, fue revocada o ya se había usado. La cookie se borra.",
                EjemplosComunes.Problema(ErroresApi.SesionExpirada, "/api/auth/refresh"));

        docs.Operacion<AuthController>(nameof(AuthController.Logout))
            .Resumen("Cerrar sesión")
            .Descripcion("""
                Revoca en la base el refresh token de la cookie y la borra. Siempre responde `204`, haya o no sesión.

                El access token ya emitido sigue siendo válido hasta que vence (como máximo 15 minutos); el cliente debe descartarlo.
                """)
            .Respuesta(204, "Sesión cerrada. Sin cuerpo.");

        docs.Operacion<AuthController>(nameof(AuthController.Me))
            .Resumen("Usuario actual")
            .Descripcion("Devuelve el usuario del access token enviado. Sirve para comprobar que el token es válido.")
            .Respuesta(200, "Datos del usuario autenticado.", new UsuarioDto(1, "demo@entrevista.local"));

        docs.Esquema<LoginDto>("Credenciales de inicio de sesión.")
            .Propiedad(nameof(LoginDto.Email), "Email del usuario. No distingue mayúsculas.", "demo@entrevista.local")
            .Propiedad(nameof(LoginDto.Password), "Contraseña.", "Demo#2026");

        docs.Esquema<SesionDto>("Sesión iniciada o renovada. El refresh token no va aquí: viaja en la cookie.")
            .Propiedad(nameof(SesionDto.AccessToken), "JWT para el header `Authorization: Bearer <token>`.")
            .Propiedad(nameof(SesionDto.ExpiraEn), "Vencimiento del access token, en UTC.")
            .Propiedad(nameof(SesionDto.Usuario), "Usuario de la sesión.");

        docs.Esquema<UsuarioDto>("Usuario autenticado.")
            .Propiedad(nameof(UsuarioDto.Id), "Id del usuario (claim `sub` del token).", 1)
            .Propiedad(nameof(UsuarioDto.Email), "Email del usuario (claim `email`).", "demo@entrevista.local");
    }
}
