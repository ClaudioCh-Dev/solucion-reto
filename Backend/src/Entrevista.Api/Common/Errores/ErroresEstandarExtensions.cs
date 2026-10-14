using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Entrevista.Api.Common.Errores;

// Todas las respuestas de error salen con el mismo formato ProblemDetails (RFC 9457, application/problem+json):
//
//   { type, title, status, detail?, instance, code, traceId, errors? }
//
// - title: texto en español para el usuario. code: identificador estable (ver ErroresApi).
// - traceId: el mismo que queda en el log, para encontrar el error en el servidor.
// - errors: solo en 400 de validación, con los mensajes por campo (claves en camelCase, como el JSON).
public static class ErroresEstandarExtensions
{
    public const string ClaveCodigo = "code";
    public const string ClaveTraceId = "traceId";

    // Títulos en inglés que ASP.NET pone por su cuenta (validación automática y UseExceptionHandler).
    private static readonly HashSet<string> TitulosPorDefecto =
    [
        "One or more validation errors occurred.",
        "An error occurred while processing your request."
    ];

    public static IServiceCollection AddErroresEstandar(this IServiceCollection services) =>
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context => Completar(context.ProblemDetails, context.HttpContext));

    // Va al inicio del pipeline para envolver todo lo demás.
    public static WebApplication UseErroresEstandar(this WebApplication app)
    {
        app.UseExceptionHandler(); // excepción no controlada → 500 con el formato estándar (el detalle queda solo en el log)
        app.UseStatusCodePages(); // errores sin cuerpo (401 del JWT, ruta inexistente, 405…) → mismo formato
        return app;
    }

    // Completa lo que falte sin pisar lo que ya puso quien creó el error.
    public static void Completar(ProblemDetails problema, HttpContext http)
    {
        var status = problema.Status ?? http.Response.StatusCode;
        var generico = ErroresApi.PorStatus(status);

        problema.Status = status;
        problema.Type ??= ErroresApi.TipoRfc(status);
        // Los títulos por defecto de ASP.NET vienen en inglés ("Not Found"): se reemplazan por el del catálogo.
        if (string.IsNullOrEmpty(problema.Title) || problema.Title == ReasonPhrases.GetReasonPhrase(status)
            || TitulosPorDefecto.Contains(problema.Title))
            problema.Title = generico.Titulo;
        problema.Instance ??= http.Request.Path;
        problema.Extensions.TryAdd(ClaveCodigo, generico.Codigo);
        problema.Extensions[ClaveTraceId] = Activity.Current?.Id ?? http.TraceIdentifier;
    }

    // Respuesta de error desde un controller: return this.Error(ErroresApi.ProductoNoEncontrado, "No existe…");
    public static ObjectResult Error(this ControllerBase controller, ErrorApi error, string? detalle = null)
    {
        var problema = controller.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(controller.HttpContext, error.Status, error.Titulo, detail: detalle);
        problema.Extensions[ClaveCodigo] = error.Codigo;
        return ComoRespuesta(problema);
    }

    public static ObjectResult ComoRespuesta(ProblemDetails problema) =>
        new(problema) { StatusCode = problema.Status, ContentTypes = { "application/problem+json" } };
}
