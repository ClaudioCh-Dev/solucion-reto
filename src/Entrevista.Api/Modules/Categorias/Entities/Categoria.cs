using Entrevista.Api.Modules.Productos.Entities;

namespace Entrevista.Api.Modules.Categorias.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
