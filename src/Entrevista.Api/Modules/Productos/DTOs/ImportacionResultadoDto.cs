namespace Entrevista.Api.Modules.Productos.DTOs;

public record ImportacionResultadoDto(
    int Recibidos,
    int Importados,
    int Omitidos,
    int CategoriasCreadas,
    IReadOnlyList<ProductoOmitidoDto> DetalleOmitidos);

public record ProductoOmitidoDto(string? ExternalId, string Motivo);
