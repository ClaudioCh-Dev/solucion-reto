namespace Entrevista.Api.Modules.Auth.DTOs;

// ExpiraEn en UTC: el cliente puede usarlo para cerrar sesión cuando el token vence.
public record TokenDto(string Token, DateTime ExpiraEn);
