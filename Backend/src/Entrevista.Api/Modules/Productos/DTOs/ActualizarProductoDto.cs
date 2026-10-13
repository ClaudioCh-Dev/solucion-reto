using System.ComponentModel.DataAnnotations;

namespace Entrevista.Api.Modules.Productos.DTOs;

// Campos editables. ExternalId e ImagenUrl no se incluyen: vienen de FakeStore y son solo de consulta.
// Las longitudes coinciden con ProductoConfiguration.
// [Required] en un string también rechaza el texto formado solo por espacios.
public record ActualizarProductoDto(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede superar 200 caracteres.")]
    string Nombre,

    [MaxLength(2000, ErrorMessage = "La descripción no puede superar 2000 caracteres.")]
    string? Descripcion,

    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    decimal Precio,

    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número entero mayor o igual a 0.")]
    int Stock,

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    int CategoriaId);
