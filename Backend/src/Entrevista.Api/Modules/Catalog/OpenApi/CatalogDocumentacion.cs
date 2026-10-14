using Entrevista.Api.Common.Errores;
using Entrevista.Api.Common.OpenApi;
using Entrevista.Api.Modules.Catalog.Controllers;
using Entrevista.Api.Modules.Catalog.DTOs;

namespace Entrevista.Api.Modules.Catalog.OpenApi;

// Textos y ejemplos de /api/catalog en el documento OpenAPI (Scalar).
public class CatalogDocumentacion : IDocumentacionModulo
{
    public void Documentar(DocumentacionApi docs)
    {
        docs.Seccion("Catalog", "Importación del catálogo desde [Fake Store API](https://fakestoreapi.com/docs) a la base local.");

        docs.Operacion<CatalogController>(nameof(CatalogController.Importar))
            .Resumen("Importar catálogo")
            .Descripcion("""
                Descarga categorías y productos de Fake Store API y guarda en la base local lo que falta.

                - **Idempotente**: los productos se identifican por `externalId` y las categorías por nombre.
                  Ejecutarlo varias veces no duplica nada; lo ya importado aparece como omitido.
                - **Valida antes de guardar**: lo inválido (nombre vacío, precio negativo, categoría inexistente…)
                  no corta la importación; queda en `detalleOmitidos` con su motivo.
                - **Todo o nada**: se guarda en una sola transacción. Si Fake Store falla, no se guarda nada.

                En desarrollo también se ejecuta solo al arrancar el backend.
                """)
            .RespuestaConEjemplos(200, "Resumen de la importación. En cada bloque: `recibidos = importados + omitidos`.",
                new Ejemplo("primeraVez", new ImportacionResultadoDto(
                    new ResumenImportacionDto(4, 4, 0, []),
                    new ResumenImportacionDto(20, 20, 0, [])), "Primera importación"),
                new Ejemplo("yaImportado", new ImportacionResultadoDto(
                    new ResumenImportacionDto(4, 0, 4, [new("electronics", "Ya fue importada anteriormente."), new("jewelery", "Ya fue importada anteriormente.")]),
                    new ResumenImportacionDto(20, 0, 20, [new("1", "Ya fue importado anteriormente."), new("2", "Ya fue importado anteriormente.")])),
                    "Reimportación (no duplica; detalle recortado)"),
                new Ejemplo("conInvalidos", new ImportacionResultadoDto(
                    new ResumenImportacionDto(4, 4, 0, []),
                    new ResumenImportacionDto(20, 18, 2, [new("7", "Precio faltante o negativo."), new("12", "La categoría 'toys' no existe.")])),
                    "Productos inválidos omitidos"))
            .Respuesta(409, "Otra importación guardó los mismos datos al mismo tiempo. No se guardó nada; se puede reintentar.",
                EjemplosComunes.Problema(ErroresApi.ImportacionEnConflicto, "/api/catalog/import",
                    "Otra importación se ejecutó al mismo tiempo. No se guardó nada; puede reintentar."))
            .Respuesta(502, "Fake Store API no respondió, tardó más de 30 s o devolvió datos inválidos. No se guardó nada.",
                EjemplosComunes.Problema(ErroresApi.FakeStoreNoDisponible, "/api/catalog/import",
                    "No se pudo conectar con FakeStore API (/products/categories). No se guardó nada; puede reintentar."));

        docs.Esquema<ImportacionResultadoDto>("Resultado de la importación, separado en categorías y productos.");

        docs.Esquema<ResumenImportacionDto>("Conteo de un tipo de elemento importado.")
            .Propiedad(nameof(ResumenImportacionDto.Recibidos), "Elementos que devolvió Fake Store API.")
            .Propiedad(nameof(ResumenImportacionDto.Importados), "Elementos nuevos guardados en la base local.")
            .Propiedad(nameof(ResumenImportacionDto.Omitidos), "Elementos no guardados: ya existían o eran inválidos.")
            .Propiedad(nameof(ResumenImportacionDto.DetalleOmitidos), "Motivo de cada elemento omitido.");

        docs.Esquema<ElementoOmitidoDto>("Elemento que no se guardó y por qué.")
            .Propiedad(nameof(ElementoOmitidoDto.Identificador), "Nombre de la categoría o `externalId` del producto. `null` si venía vacío.", "7")
            .Propiedad(nameof(ElementoOmitidoDto.Motivo), "Por qué se omitió.", "Precio faltante o negativo.");
    }
}
