using Entrevista.Api.Common.Errores;

namespace Entrevista.Api.Common.OpenApi;

// Ejemplos de error para la documentación, con la misma forma que produce ErroresEstandarExtensions.
public static class EjemplosComunes
{
    private const string TraceId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00";

    public static object Problema(ErrorApi error, string instance, string? detalle = null) => detalle is null
        ? new { type = ErroresApi.TipoRfc(error.Status), title = error.Titulo, status = error.Status, instance, code = error.Codigo, traceId = TraceId }
        : new { type = ErroresApi.TipoRfc(error.Status), title = error.Titulo, status = error.Status, detail = detalle, instance, code = error.Codigo, traceId = TraceId };

    // Errores agrupados por campo, con las claves en camelCase como el JSON.
    public static object Validacion(string instance, params (string Campo, string Mensaje)[] errores) => new
    {
        type = ErroresApi.TipoRfc(400),
        title = ErroresApi.Validacion.Titulo,
        status = 400,
        instance,
        code = ErroresApi.Validacion.Codigo,
        traceId = TraceId,
        errors = errores.GroupBy(e => e.Campo).ToDictionary(g => g.Key, g => g.Select(e => e.Mensaje).ToArray())
    };
}
