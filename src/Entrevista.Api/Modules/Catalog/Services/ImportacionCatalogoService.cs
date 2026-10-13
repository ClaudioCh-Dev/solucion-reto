using Entrevista.Api.Data;
using Entrevista.Api.Modules.Categorias.Entities;
using Entrevista.Api.Modules.Catalog.DTOs;
using Entrevista.Api.Modules.Productos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Catalog.Services;

// Importa categorías y luego productos desde FakeStore API. Solo inserta lo que no existe
// (categorías por Nombre, productos por ExternalId), así que se puede reintentar sin duplicar.
// Todo se guarda en un único SaveChanges: si algo falla, no queda nada a medias.
public class ImportacionCatalogoService(
    AppDbContext db,
    FakeStoreClient fakeStore,
    ILogger<ImportacionCatalogoService> logger)
{
    public async Task<ImportacionResultadoDto> ImportarAsync(CancellationToken ct = default)
    {
        // Se descargan ambas listas antes de tocar la BD: si una falla, no se guarda nada.
        var categoriasApi = await fakeStore.GetCategoriasAsync(ct);
        var productosApi = await fakeStore.GetProductosAsync(ct);

        // SQL Server compara sin distinguir mayúsculas; el diccionario hace lo mismo.
        var categorias = await db.Categorias
            .ToDictionaryAsync(c => c.Nombre, StringComparer.OrdinalIgnoreCase, ct);

        var resumenCategorias = ImportarCategorias(categoriasApi, categorias);
        var resumenProductos = await ImportarProductosAsync(productosApi, categorias, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Importación FakeStore. Categorías: {CatRecibidas} recibidas, {CatImportadas} importadas, {CatOmitidas} omitidas. " +
            "Productos: {ProdRecibidos} recibidos, {ProdImportados} importados, {ProdOmitidos} omitidos.",
            resumenCategorias.Recibidos, resumenCategorias.Importados, resumenCategorias.Omitidos,
            resumenProductos.Recibidos, resumenProductos.Importados, resumenProductos.Omitidos);

        return new ImportacionResultadoDto(resumenCategorias, resumenProductos);
    }

    private ResumenImportacionDto ImportarCategorias(
        List<string?> categoriasApi, Dictionary<string, Categoria> categorias)
    {
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var omitidas = new List<ElementoOmitidoDto>();
        var importadas = 0;

        foreach (var nombreApi in categoriasApi)
        {
            var nombre = nombreApi?.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                omitidas.Add(new(null, "Nombre vacío."));
                continue;
            }
            if (nombre.Length > 100)
            {
                omitidas.Add(new(nombre, "El nombre supera 100 caracteres."));
                continue;
            }
            if (!vistas.Add(nombre))
            {
                omitidas.Add(new(nombre, "Repetida en la respuesta de la API."));
                continue;
            }
            if (categorias.ContainsKey(nombre))
            {
                omitidas.Add(new(nombre, "Ya fue importada anteriormente."));
                continue;
            }

            var categoria = new Categoria { Nombre = nombre };
            db.Categorias.Add(categoria);
            categorias[nombre] = categoria;
            importadas++;
        }

        return new ResumenImportacionDto(categoriasApi.Count, importadas, omitidas.Count, omitidas);
    }

    private async Task<ResumenImportacionDto> ImportarProductosAsync(
        List<FakeStoreProductDto?> productosApi, Dictionary<string, Categoria> categorias, CancellationToken ct)
    {
        var existentes = await db.Productos.Select(p => p.ExternalId).ToHashSetAsync(ct);
        var vistos = new HashSet<string>();
        var omitidos = new List<ElementoOmitidoDto>();
        var importados = 0;

        foreach (var p in productosApi)
        {
            if (p is null)
            {
                omitidos.Add(new(null, "Elemento vacío."));
                continue;
            }

            var externalId = p.Id > 0 ? p.Id.ToString() : null;
            var error = Validar(p);
            if (error is not null)
            {
                omitidos.Add(new(externalId, error));
                continue;
            }

            if (existentes.Contains(externalId!))
            {
                omitidos.Add(new(externalId, "Ya fue importado anteriormente."));
                continue;
            }
            if (!vistos.Add(externalId!))
            {
                omitidos.Add(new(externalId, "Repetido en la respuesta de la API."));
                continue;
            }

            // La categoría debe venir de /products/categories (o existir ya en la BD).
            var nombreCategoria = p.Category!.Trim();
            if (!categorias.TryGetValue(nombreCategoria, out var categoria))
            {
                omitidos.Add(new(externalId, $"La categoría '{nombreCategoria}' no existe."));
                continue;
            }

            db.Productos.Add(new Producto
            {
                ExternalId = externalId!,
                Nombre = p.Title!.Trim(),
                Descripcion = p.Description,
                Precio = p.Price!.Value,
                Stock = 0,
                ImagenUrl = p.Image,
                Categoria = categoria
            });
            importados++;
        }

        return new ResumenImportacionDto(productosApi.Count, importados, omitidos.Count, omitidos);
    }

    // Devuelve el motivo si el producto no es válido, o null si se puede guardar.
    // Los límites coinciden con ProductoConfiguration y CategoriaConfiguration.
    private static string? Validar(FakeStoreProductDto p)
    {
        if (p.Id <= 0) return "Id inválido.";
        if (string.IsNullOrWhiteSpace(p.Title)) return "Falta el nombre.";
        if (p.Title.Trim().Length > 200) return "El nombre supera 200 caracteres.";
        if (p.Price is null or < 0) return "Precio faltante o negativo.";
        if (string.IsNullOrWhiteSpace(p.Category)) return "Falta la categoría.";
        if (p.Description?.Length > 2000) return "La descripción supera 2000 caracteres.";
        if (p.Image?.Length > 2048) return "La URL de la imagen supera 2048 caracteres.";
        return null;
    }
}
