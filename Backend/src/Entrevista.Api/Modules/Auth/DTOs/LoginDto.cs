using System.ComponentModel.DataAnnotations;

namespace Entrevista.Api.Modules.Auth.DTOs;

public record LoginDto(
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [MaxLength(256, ErrorMessage = "El correo no puede superar 256 caracteres.")]
    string Email,

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede superar 100 caracteres.")]
    string Password);
