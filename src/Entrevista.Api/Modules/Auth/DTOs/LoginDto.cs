using System.ComponentModel.DataAnnotations;

namespace Entrevista.Api.Modules.Auth.DTOs;

public record LoginDto(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string Password);
