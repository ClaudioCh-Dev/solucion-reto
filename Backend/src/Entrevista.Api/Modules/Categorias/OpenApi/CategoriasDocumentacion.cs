using Entrevista.Api.Common.OpenApi;
using Entrevista.Api.Modules.Categorias.Controllers;
using Entrevista.Api.Modules.Categorias.DTOs;

namespace Entrevista.Api.Modules.Categorias.OpenApi;

// Textos y ejemplos de /api/categories en el documento OpenAPI (Scalar).
public class CategoriasDocumentacion : IDocumentacionModulo
{
    public void Documentar(DocumentacionApi docs)
    {
        docs.Seccion("Categorias", "Categorías importadas de Fake Store API, con su nombre original en inglés.");

        docs.Operacion<CategoriasController>(nameof(CategoriasController.Listar))
            .Resumen("Listar categorías")
            .Descripcion("Categorías de la base local ordenadas por nombre. Las usa el selector del formulario de edición de producto.")
            .Respuesta(200, "Lista de categorías.", new[]
            {
                new CategoriaDto(3, "electronics"),
                new CategoriaDto(2, "jewelery"),
                new CategoriaDto(1, "men's clothing"),
                new CategoriaDto(4, "women's clothing")
            });

        docs.Esquema<CategoriaDto>("Categoría del catálogo local.")
            .Propiedad(nameof(CategoriaDto.Id), "Id local. Es el que se envía como `categoriaId` al editar un producto.")
            .Propiedad(nameof(CategoriaDto.Nombre), "Nombre tal como viene de Fake Store API (único).");
    }
}
