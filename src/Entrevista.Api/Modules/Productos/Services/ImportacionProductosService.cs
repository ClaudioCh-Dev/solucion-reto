using Entrevista.Api.Data;
using Entrevista.Api.Modules.Categorias.Entities;
using Entrevista.Api.Modules.Productos.DTOs;
using Entrevista.Api.Modules.Productos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Productos.Services;

// Importa productos desde FakeStore API. Solo inserta los que no existen (por ExternalId),
// así que se puede reintentar sin duplicar. Todo se guarda en un único SaveChanges:
// si algo falla, no queda nada a medias.
public class ImportacionProductosService(
    AppDbContext db,
    FakeStoreClient fakeStore,
    ILogger<ImportacionProductosService> logger)
{
    public async Task<ImportacionResultadoDto> ImportarAsync(CancellationToken ct = default)
    {
        var productosApi = await fakeStore.GetProductosAsync(ct);

        var existentes = await db.Productos.Select(p => p.ExternalId).ToHashSetAsync(ct);
        // SQL Server compara sin distinguir mayúsculas; el diccionario hace lo mismo.
        var categorias = await db.Categorias
            .ToDictionaryAsync(c => c.Nombre, StringComparer.OrdinalIgnoreCase, ct);

        var vistos = new HashSet<string>();
        var omitidos = new List<ProductoOmitidoDto>();
        var importados = 0;
        var categoriasCreadas = 0;

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

            var nombreCategoria = p.Category!.Trim();
            if (!categorias.TryGetValue(nombreCategoria, out var categoria))
            {
                categoria = new Categoria { Nombre = nombreCategoria };
                db.Categorias.Add(categoria);
                categorias[nombreCategoria] = categoria;
                categoriasCreadas++;
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

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Importación FakeStore: {Recibidos} recibidos, {Importados} importados, {Omitidos} omitidos.",
            productosApi.Count, importados, omitidos.Count);

        return new ImportacionResultadoDto(
            productosApi.Count, importados, omitidos.Count, categoriasCreadas, omitidos);
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
        if (p.Category.Trim().Length > 100) return "La categoría supera 100 caracteres.";
        if (p.Description?.Length > 2000) return "La descripción supera 2000 caracteres.";
        if (p.Image?.Length > 2048) return "La URL de la imagen supera 2048 caracteres.";
        return null;
    }
}
