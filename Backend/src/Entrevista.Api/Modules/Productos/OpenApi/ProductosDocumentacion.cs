using Entrevista.Api.Common.Errores;
using Entrevista.Api.Common.OpenApi;
using Entrevista.Api.Modules.Productos.Controllers;
using Entrevista.Api.Modules.Productos.DTOs;

namespace Entrevista.Api.Modules.Productos.OpenApi;

// Textos y ejemplos de /api/products en el documento OpenAPI (Scalar).
public class ProductosDocumentacion : IDocumentacionModulo
{
    private static readonly ProductoDto Mochila = new(
        1, "1", "Fjallraven - Foldsack No. 1 Backpack, Fits 15 Laptops",
        "Your perfect pack for everyday use and walks in the forest.",
        109.95m, 10, "https://fakestoreapi.com/img/81fPKd-2AYL._AC_SL1500_t.png", 1, "men's clothing");

    private static readonly ProductoDto Camiseta = new(
        2, "2", "Mens Casual Premium Slim Fit T-Shirts",
        "Slim-fitting style, contrast raglan long sleeve, three-button henley placket.",
        22.3m, 0, "https://fakestoreapi.com/img/71-3HjGNDUL._AC_SY879._SX._UX._SY._UY_t.png", 1, "men's clothing");

    public void Documentar(DocumentacionApi docs)
    {
        docs.Seccion("Productos", "Catálogo guardado en la base local. Se llena con **Catalog → Importar catálogo**; " +
                                  "aquí nunca se consulta Fake Store API.");

        docs.Operacion<ProductosController>(nameof(ProductosController.Listar))
            .Resumen("Listar productos")
            .Descripcion("Devuelve todos los productos de la base local, con el nombre de su categoría.")
            .Respuesta(200, "Lista de productos (vacía si todavía no se importó el catálogo).", new[] { Mochila, Camiseta });

        docs.Operacion<ProductosController>(nameof(ProductosController.Obtener))
            .Resumen("Obtener un producto")
            .Descripcion("Devuelve un producto por su `id` local (no el `externalId` de Fake Store).")
            .Parametro("id", "Id local del producto.", 1)
            .Respuesta(200, "El producto.", Mochila)
            .Respuesta(404, "No existe un producto con ese `id`.",
                EjemplosComunes.Problema(ErroresApi.ProductoNoEncontrado, "/api/products/999", "No existe un producto con Id 999."));

        docs.Operacion<ProductosController>(nameof(ProductosController.Actualizar))
            .Resumen("Editar un producto")
            .Descripcion("""
                Guarda en la base local los campos editables y devuelve el producto actualizado.

                **Reglas** (mensajes en español por campo):
                - `nombre`: obligatorio (no vale solo con espacios), hasta 200 caracteres.
                - `descripcion`: opcional, hasta 2000 caracteres. `null` la deja vacía.
                - `precio`: mayor o igual a 0.
                - `stock`: número entero mayor o igual a 0 (`1.5` se rechaza).
                - `categoriaId`: debe existir en **Categorías**.

                `externalId` e `imagenUrl` vienen de Fake Store y no se pueden editar.
                """)
            .Parametro("id", "Id local del producto a editar.", 1)
            .CuerpoConEjemplos(
                new Ejemplo("completo", new ActualizarProductoDto(
                    "Mochila Fjallraven Foldsack No. 1", "Mochila para el día a día, con funda acolchada para laptop de 15\".",
                    99.90m, 25, 1), "Todos los campos"),
                new Ejemplo("sinDescripcion", new ActualizarProductoDto("Mochila Fjallraven", null, 99.90m, 25, 1),
                    "Sin descripción (null)"))
            .Respuesta(200, "Cambios guardados. Devuelve el producto actualizado.",
                Mochila with { Nombre = "Mochila Fjallraven Foldsack No. 1", Precio = 99.90m, Stock = 25,
                               Descripcion = "Mochila para el día a día, con funda acolchada para laptop de 15\"." })
            .RespuestaConEjemplos(400, "Algún dato no es válido. `errors` trae los mensajes por campo (claves en camelCase, como el JSON).",
                new Ejemplo("reglas", EjemplosComunes.Validacion("/api/products/1",
                    ("nombre", "El nombre es obligatorio."),
                    ("precio", "El precio debe ser mayor o igual a 0."),
                    ("categoriaId", "Selecciona una categoría.")), "Campos que no cumplen las reglas"),
                new Ejemplo("categoriaInexistente", EjemplosComunes.Validacion("/api/products/1",
                    ("categoriaId", "No existe una categoría con Id 999.")), "La categoría no existe"),
                new Ejemplo("formatoInvalido", EjemplosComunes.Validacion("/api/products/1",
                    ("stock", "El valor no tiene un formato válido.")), "Tipo de dato incorrecto (ej. \"stock\": 1.5)"))
            .Respuesta(404, "No existe un producto con ese `id`.",
                EjemplosComunes.Problema(ErroresApi.ProductoNoEncontrado, "/api/products/999", "No existe un producto con Id 999."));

        docs.Esquema<ProductoDto>("Producto del catálogo local.")
            .Propiedad(nameof(ProductoDto.Id), "Id local, asignado por la base de datos.")
            .Propiedad(nameof(ProductoDto.ExternalId), "Id del producto en Fake Store API. Evita duplicados al reimportar.")
            .Propiedad(nameof(ProductoDto.Nombre), "Nombre del producto.")
            .Propiedad(nameof(ProductoDto.Descripcion), "Descripción, o `null` si no tiene.")
            .Propiedad(nameof(ProductoDto.Precio), "Precio en USD.")
            .Propiedad(nameof(ProductoDto.Stock), "Unidades disponibles. Al importar empieza en 0.")
            .Propiedad(nameof(ProductoDto.ImagenUrl), "URL de la imagen en Fake Store. Solo lectura.")
            .Propiedad(nameof(ProductoDto.CategoriaId), "Id local de la categoría.")
            .Propiedad(nameof(ProductoDto.CategoriaNombre), "Nombre de la categoría, para mostrar sin otra consulta.");

        docs.Esquema<ActualizarProductoDto>("Campos editables de un producto.")
            .Propiedad(nameof(ActualizarProductoDto.Nombre), "Obligatorio, hasta 200 caracteres.", "Mochila Fjallraven Foldsack No. 1")
            .Propiedad(nameof(ActualizarProductoDto.Descripcion), "Opcional, hasta 2000 caracteres.")
            .Propiedad(nameof(ActualizarProductoDto.Precio), "Mayor o igual a 0.", 99.90m)
            .Propiedad(nameof(ActualizarProductoDto.Stock), "Entero mayor o igual a 0.", 25)
            .Propiedad(nameof(ActualizarProductoDto.CategoriaId), "Id de una categoría existente (ver `GET /api/categories`).", 1);
    }
}
