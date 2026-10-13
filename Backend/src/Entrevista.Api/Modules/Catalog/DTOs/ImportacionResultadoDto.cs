namespace Entrevista.Api.Modules.Catalog.DTOs;

public record ImportacionResultadoDto(ResumenImportacionDto Categorias, ResumenImportacionDto Productos);

// Recibidos = Importados + Omitidos.
public record ResumenImportacionDto(
    int Recibidos,
    int Importados,
    int Omitidos,
    IReadOnlyList<ElementoOmitidoDto> DetalleOmitidos);

// Identificador: nombre de la categoría o ExternalId del producto.
public record ElementoOmitidoDto(string? Identificador, string Motivo);
