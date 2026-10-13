using Entrevista.Api.Common;
using Entrevista.Api.Modules.Productos.DTOs;
using Entrevista.Api.Modules.Productos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrevista.Api.Modules.Productos.Controllers;

[ApiController]
[Authorize]
[Route("products")]
public class ProductosController(ProductoService productos) : ControllerBase
{
    // GET api/products
    [HttpGet]
    [ProducesResponseType<List<ProductoDto>>(StatusCodes.Status200OK)]
    public async Task<List<ProductoDto>> Listar(CancellationToken ct) =>
        await productos.ListarAsync(ct);

    // GET api/products/5
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDto>> Obtener(int id, CancellationToken ct) =>
        await productos.ObtenerAsync(id, ct) is { } producto ? producto : NotFound();

    // PUT api/products/5
    // Guarda los cambios en la base local. Devuelve el producto actualizado.
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDto>> Actualizar(int id, ActualizarProductoDto dto, CancellationToken ct)
    {
        var (resultado, producto) = await productos.ActualizarAsync(id, dto, ct);

        switch (resultado)
        {
            case ResultadoActualizacion.ProductoNoEncontrado:
                return NotFound();
            case ResultadoActualizacion.CategoriaNoExiste:
                // Mismo formato de error 400 que las validaciones automáticas de [ApiController].
                ModelState.AddModelError(nameof(dto.CategoriaId), $"No existe una categoría con Id {dto.CategoriaId}.");
                return ValidacionResponseFactory.Crear(ControllerContext);
            default:
                return producto!;
        }
    }
}
