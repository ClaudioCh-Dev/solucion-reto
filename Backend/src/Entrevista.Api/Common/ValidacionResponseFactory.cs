using Microsoft.AspNetCore.Mvc;

namespace Entrevista.Api.Common;

// Respuesta 400 de validación (la usan [ApiController] y ValidationProblem()) con textos en español.
// Si el JSON no se puede leer (ej. "stock": 1.5 o "precio": "abc"), System.Text.Json deja un error
// técnico en inglés con clave "$.stock": se reemplaza por un mensaje comprensible en el campo "Stock".
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
                e => e.Key,
                e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray());

        var problema = new ValidationProblemDetails(errores)
        {
            Title = "Uno o más datos no son válidos.",
            Status = StatusCodes.Status400BadRequest
        };
        return new BadRequestObjectResult(problema);
    }

    // "$.stock" → "Stock"; "$" (cuerpo vacío o JSON mal formado) → "".
    private static string NombreDeCampo(string clave)
    {
        var campo = clave.TrimStart('$', '.');
        return campo.Length == 0 ? string.Empty : char.ToUpperInvariant(campo[0]) + campo[1..];
    }
}
