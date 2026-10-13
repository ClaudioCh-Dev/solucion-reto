using Entrevista.Api.Modules.Catalog.DTOs;
using Entrevista.Api.Modules.Catalog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Catalog.Controllers;

[ApiController]
[Authorize]
[Route("catalog")]
public class CatalogController(ImportacionCatalogoService importacion) : ControllerBase
{
    // POST api/catalog/import
    // Importa categorías y productos de FakeStore. Se puede llamar varias veces: solo agrega lo que falta.
    [HttpPost("import")]
    [ProducesResponseType<ImportacionResultadoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Importar(CancellationToken ct)
    {
        try
        {
            return Ok(await importacion.ImportarAsync(ct));
        }
        catch (FakeStoreException ex)
        {
            return Problem(
                title: "No se pudo importar desde FakeStore API.",
                detail: $"{ex.Message} No se guardó nada; puede reintentar.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // 2601/2627 = clave duplicada: otra importación simultánea guardó los mismos datos.
            return Problem(
                title: "Conflicto al guardar la importación.",
                detail: "Otra importación se ejecutó al mismo tiempo. No se guardó nada; puede reintentar.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
