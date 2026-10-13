namespace Entrevista.Api.Modules.Productos.DTOs;

public record ProductoDto(
    int Id,
    string ExternalId,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string? ImagenUrl,
    int CategoriaId,
    string CategoriaNombre);
