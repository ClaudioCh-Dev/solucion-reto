using System.Linq.Expressions;
using Entrevista.Api.Data;
using Entrevista.Api.Modules.Productos.DTOs;
using Entrevista.Api.Modules.Productos.Entities;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Productos.Services;

public enum ResultadoActualizacion { Actualizado, ProductoNoEncontrado, CategoriaNoExiste }

public class ProductoService(AppDbContext db)
{
    // Proyección compartida: EF la traduce a SQL y solo trae las columnas necesarias (con JOIN a Categorias).
    private static readonly Expression<Func<Producto, ProductoDto>> AProductoDto = p => new ProductoDto(
        p.Id, p.ExternalId, p.Nombre, p.Descripcion, p.Precio, p.Stock, p.ImagenUrl,
        p.CategoriaId, p.Categoria.Nombre);

    public Task<List<ProductoDto>> ListarAsync(CancellationToken ct = default) =>
        db.Productos
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(AProductoDto)
            .ToListAsync(ct);

    public Task<ProductoDto?> ObtenerAsync(int id, CancellationToken ct = default) =>
        db.Productos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(AProductoDto)
            .FirstOrDefaultAsync(ct);

    public async Task<(ResultadoActualizacion Resultado, ProductoDto? Producto)> ActualizarAsync(
        int id, ActualizarProductoDto dto, CancellationToken ct = default)
    {
        var producto = await db.Productos.FindAsync([id], ct);
        if (producto is null)
            return (ResultadoActualizacion.ProductoNoEncontrado, null);

        if (!await db.Categorias.AnyAsync(c => c.Id == dto.CategoriaId, ct))
            return (ResultadoActualizacion.CategoriaNoExiste, null);

        producto.Nombre = dto.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.CategoriaId = dto.CategoriaId;

        // El producto se obtuvo con tracking: EF detecta qué propiedades cambiaron y genera el UPDATE.
        await db.SaveChangesAsync(ct);

        return (ResultadoActualizacion.Actualizado, await ObtenerAsync(id, ct));
    }
}
