using Entrevista.Api.Modules.Categorias.DTOs;
using Entrevista.Api.Modules.Categorias.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrevista.Api.Modules.Categorias.Controllers;

[ApiController]
[Authorize]
[Route("categories")]
public class CategoriasController(CategoriaService categorias) : ControllerBase
{
    // GET api/categories
    // Lista de categorías ordenada por nombre (para el selector del formulario de producto).
    [HttpGet]
    [ProducesResponseType<List<CategoriaDto>>(StatusCodes.Status200OK)]
    public async Task<List<CategoriaDto>> Listar(CancellationToken ct) =>
        await categorias.ListarAsync(ct);
}
