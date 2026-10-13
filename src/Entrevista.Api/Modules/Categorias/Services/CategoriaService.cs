using Entrevista.Api.Data;
using Entrevista.Api.Modules.Categorias.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Entrevista.Api.Modules.Categorias.Services;

public class CategoriaService(AppDbContext db)
{
    public Task<List<CategoriaDto>> ListarAsync(CancellationToken ct = default) =>
        db.Categorias
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre))
            .ToListAsync(ct);
}
