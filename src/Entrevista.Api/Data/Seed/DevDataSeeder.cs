using Entrevista.Api.Modules.Categorias.Entities;
using Entrevista.Api.Modules.Productos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Data.Seed;

// Carga categorías y productos desde FakeStore API. Solo inserta lo que falta,
// así que se puede ejecutar en cada arranque sin duplicar datos.
public class DevDataSeeder(AppDbContext db, FakeStoreClient fakeStore, ILogger<DevDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var nombresApi = await fakeStore.GetCategoriasAsync(ct);
        var productosApi = await fakeStore.GetProductosAsync(ct);

        var categorias = await db.Categorias.ToDictionaryAsync(c => c.Nombre, ct);
        foreach (var nombre in nombresApi.Concat(productosApi.Select(p => p.Category)).Distinct())
        {
            if (categorias.ContainsKey(nombre)) continue;
            var categoria = new Categoria { Nombre = nombre };
            db.Categorias.Add(categoria);
            categorias[nombre] = categoria;
        }

        var externalIds = await db.Productos.Select(p => p.ExternalId).ToHashSetAsync(ct);
        var nuevos = 0;
        foreach (var p in productosApi)
        {
            var externalId = p.Id.ToString();
            if (externalIds.Contains(externalId)) continue;

            db.Productos.Add(new Producto
            {
                ExternalId = externalId,
                Nombre = p.Title.Trim(),
                Descripcion = p.Description,
                Precio = p.Price,
                Stock = 0,
                ImagenUrl = p.Image,
                Categoria = categorias[p.Category]
            });
            nuevos++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed FakeStore: {Categorias} categorías, {Nuevos} productos nuevos.",
            categorias.Count, nuevos);
    }
}
