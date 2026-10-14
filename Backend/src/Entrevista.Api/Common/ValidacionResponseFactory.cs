using Entrevista.Api.Common.Errores;
using Microsoft.AspNetCore.Mvc;

namespace Entrevista.Api.Common;

// Respuesta 400 de validación (la usan [ApiController] y ValidationProblem()) con textos en español.
// Si el JSON no se puede leer (ej. "stock": 1.5 o "precio": "abc"), System.Text.Json deja un error
// técnico en inglés con clave "$.stock": se reemplaza por un mensaje comprensible en el campo "stock".
// Sale con el formato estándar de errores (ErroresEstandarExtensions) y las claves en camelCase, como el JSON.
public static class ValidacionResponseFactory
{
    public static ActionResult Crear(ActionContext context)
    {
        var estado = context.ModelState.Where(e => e.Value is { Errors.Count: > 0 }).ToList();

        // Con errores de formato no se llegan a ejecutar las demás validaciones: solo se informan esos.
        var erroresDeFormato = estado.Where(e => e.Key.StartsWith('$')).ToList();

        var errores = erroresDeFormato.Count > 0
            ? erroresDeFormato.ToDictionary(
                e => NombreDeCampo(e.Key),
                _ => new[] { "El valor no tiene un formato válido." })
            : estado.ToDictionary(
                e => CamelCase(e.Key),
                e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray());

        var problema = new ValidationProblemDetails(errores)
        {
            Title = ErroresApi.Validacion.Titulo,
            Status = ErroresApi.Validacion.Status
        };
        problema.Extensions[ErroresEstandarExtensions.ClaveCodigo] = ErroresApi.Validacion.Codigo;
        ErroresEstandarExtensions.Completar(problema, context.HttpContext);
        return ErroresEstandarExtensions.ComoRespuesta(problema);
    }

    // "$.stock" → "stock"; "$" (cuerpo vacío o JSON mal formado) → "".
    private static string NombreDeCampo(string clave) => CamelCase(clave.TrimStart('$', '.'));

    // "CategoriaId" → "categoriaId"; "Usuario.Email" → "usuario.email".
    private static string CamelCase(string clave) => string.Join('.', clave.Split('.')
        .Select(parte => parte.Length == 0 ? parte : char.ToLowerInvariant(parte[0]) + parte[1..]));
}
