using System.ComponentModel.DataAnnotations;

namespace Entrevista.Api.Modules.Productos.DTOs;

// Campos editables. ExternalId no se incluye: identifica al producto en FakeStore y no se modifica.
// Las longitudes coinciden con ProductoConfiguration.
public record ActualizarProductoDto(
    [Required, MaxLength(200)] string Nombre,
    [MaxLength(2000)] string? Descripcion,
    [Range(typeof(decimal), "0", "9999999999999999")] decimal Precio,
    [Range(0, int.MaxValue)] int Stock,
    [MaxLength(2048), Url] string? ImagenUrl,
    [Range(1, int.MaxValue)] int CategoriaId);
