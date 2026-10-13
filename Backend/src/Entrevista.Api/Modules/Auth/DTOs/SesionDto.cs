namespace Entrevista.Api.Modules.Auth.DTOs;

// Respuesta de login y refresh. El refresh token NO va aquí: viaja en una cookie HttpOnly.
// ExpiraEn (UTC) es el vencimiento del access token; el cliente lo renueva antes con /refresh.
public record SesionDto(string AccessToken, DateTime ExpiraEn, UsuarioDto Usuario);
