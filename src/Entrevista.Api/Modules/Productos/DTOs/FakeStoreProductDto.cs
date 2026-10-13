namespace Entrevista.Api.Modules.Productos.DTOs;

// Campos nullables: la API externa puede omitirlos y se validan antes de guardar.
public record FakeStoreProductDto(
    int Id,
    string? Title,
    decimal? Price,
    string? Description,
    string? Category,
    string? Image);
