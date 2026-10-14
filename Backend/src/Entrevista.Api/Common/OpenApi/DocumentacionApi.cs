using Microsoft.AspNetCore.Mvc;

namespace Entrevista.Api.Common.OpenApi;

// Cada módulo describe sus endpoints en una clase aparte (Modules/*/OpenApi) que implementa esta interfaz.
// Así los controllers solo tienen la lógica HTTP y los textos y ejemplos de la documentación viven fuera.
// Se descubren solas al arrancar (ver OpenApiExtensions.AddDocumentacionApi).
public interface IDocumentacionModulo
{
    void Documentar(DocumentacionApi docs);
}

// Registro de la documentación: secciones (tags), operaciones y esquemas.
// Lo lee DocumentacionTransformer al generar /openapi/v1.json.
public class DocumentacionApi
{
    internal Dictionary<string, string> Secciones { get; } = [];
    internal Dictionary<(Type Controller, string Accion), OperacionDoc> Operaciones { get; } = [];
    internal Dictionary<Type, EsquemaDoc> Esquemas { get; } = [];

    // Descripción de una sección del menú. El nombre es el del controller sin "Controller" (ej. "Productos").
    public DocumentacionApi Seccion(string nombre, string descripcion)
    {
        Secciones[nombre] = descripcion;
        return this;
    }

    // Uso: docs.Operacion<ProductosController>(nameof(ProductosController.Listar)). Con nameof, renombrar la acción no rompe la documentación.
    public OperacionDoc Operacion<TController>(string accion) where TController : ControllerBase
    {
        var doc = new OperacionDoc();
        Operaciones[(typeof(TController), accion)] = doc;
        return doc;
    }

    public EsquemaDoc Esquema<T>(string descripcion)
    {
        var doc = new EsquemaDoc(descripcion);
        Esquemas[typeof(T)] = doc;
        return doc;
    }
}

// Un ejemplo con nombre. Se usa cuando una respuesta o un cuerpo tiene varios casos (ej. distintos errores 400).
public record Ejemplo(string Nombre, object Valor, string? Resumen = null);

public record RespuestaDoc(string Descripcion, IReadOnlyList<Ejemplo> Ejemplos);

public record ParametroDoc(string Descripcion, object? Ejemplo);

public class OperacionDoc
{
    internal string? TextoResumen { get; private set; }
    internal string? TextoDescripcion { get; private set; }
    internal Dictionary<string, ParametroDoc> Parametros { get; } = [];
    internal List<Ejemplo> EjemplosCuerpo { get; } = [];
    internal Dictionary<int, RespuestaDoc> Respuestas { get; } = [];

    // Título corto que aparece en el menú.
    public OperacionDoc Resumen(string texto)
    {
        TextoResumen = texto;
        return this;
    }

    // Explicación larga. Admite Markdown.
    public OperacionDoc Descripcion(string markdown)
    {
        TextoDescripcion = markdown;
        return this;
    }

    public OperacionDoc Parametro(string nombre, string descripcion, object? ejemplo = null)
    {
        Parametros[nombre] = new ParametroDoc(descripcion, ejemplo);
        return this;
    }

    public OperacionDoc Cuerpo(object ejemplo) => CuerpoConEjemplos(new Ejemplo("ejemplo", ejemplo));

    public OperacionDoc CuerpoConEjemplos(params Ejemplo[] ejemplos)
    {
        EjemplosCuerpo.AddRange(ejemplos);
        return this;
    }

    public OperacionDoc Respuesta(int status, string descripcion, object? ejemplo = null) =>
        RespuestaConEjemplos(status, descripcion, ejemplo is null ? [] : [new Ejemplo("ejemplo", ejemplo)]);

    public OperacionDoc RespuestaConEjemplos(int status, string descripcion, params Ejemplo[] ejemplos)
    {
        Respuestas[status] = new RespuestaDoc(descripcion, ejemplos);
        return this;
    }
}

public class EsquemaDoc(string descripcion)
{
    internal string Descripcion { get; } = descripcion;
    internal Dictionary<string, (string Descripcion, object? Ejemplo)> Propiedades { get; } = [];

    // El nombre es el de la propiedad en C# (ej. nameof(ProductoDto.Precio)); se pasa a camelCase como en el JSON.
    public EsquemaDoc Propiedad(string nombre, string descripcion, object? ejemplo = null)
    {
        Propiedades[char.ToLowerInvariant(nombre[0]) + nombre[1..]] = (descripcion, ejemplo);
        return this;
    }
}
