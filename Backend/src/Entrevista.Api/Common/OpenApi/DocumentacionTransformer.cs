using System.Text.Json;
using System.Text.Json.Nodes;
using Entrevista.Api.Common.Errores;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Entrevista.Api.Common.OpenApi;

// Aplica al documento OpenAPI lo registrado en DocumentacionApi: datos generales, secciones,
// resúmenes, descripciones, ejemplos de cuerpo y de respuesta, y descripciones de los esquemas.
public class DocumentacionTransformer(DocumentacionApi docs)
    : IOpenApiDocumentTransformer, IOpenApiOperationTransformer, IOpenApiSchemaTransformer
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Tejidos Alma · API del catálogo",
            Version = "v1",
            Description = DescripcionGeneral
        };

        document.Tags = docs.Secciones
            .Select(s => new OpenApiTag { Name = s.Key, Description = s.Value })
            .ToList();
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // Una sola representación por respuesta: JSON para éxito y ProblemDetails para errores
        // (ASP.NET declara además text/plain y text/json, que la API nunca usa).
        if (operation.RequestBody is { } cuerpo)
            cuerpo.Content = UnSoloTipo(cuerpo.Content, Json);
        foreach (var (status, respuesta) in operation.Responses)
            respuesta.Content = UnSoloTipo(respuesta.Content, status.StartsWith('2') ? Json : ProblemJson);

        // Cualquier endpoint puede fallar de forma inesperada: siempre el mismo 500, sin detalles internos.
        operation.Responses.TryAdd("500", RespuestaDeError(
            "Error inesperado en el servidor. El detalle queda en el log, buscable por `traceId`.",
            EjemplosComunes.Problema(ErroresApi.ErrorInterno, "/" + context.Description.RelativePath)));

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor accion
            || !docs.Operaciones.TryGetValue((accion.ControllerTypeInfo.AsType(), accion.ActionName), out var doc))
            return Task.CompletedTask;

        operation.Summary = doc.TextoResumen ?? operation.Summary;
        operation.Description = doc.TextoDescripcion ?? operation.Description;

        foreach (var parametro in operation.Parameters ?? [])
        {
            if (!doc.Parametros.TryGetValue(parametro.Name, out var p)) continue;
            parametro.Description = p.Descripcion;
            if (p.Ejemplo is not null) parametro.Example = ComoOpenApi(p.Ejemplo);
        }

        if (doc.EjemplosCuerpo.Count > 0 && operation.RequestBody?.Content.TryGetValue(Json, out var tipoCuerpo) == true)
            AsignarEjemplos(tipoCuerpo, doc.EjemplosCuerpo);

        foreach (var (status, r) in doc.Respuestas)
        {
            var clave = status.ToString();
            if (!operation.Responses.TryGetValue(clave, out var respuesta))
                operation.Responses[clave] = respuesta = new OpenApiResponse();

            respuesta.Description = r.Descripcion;
            if (r.Ejemplos.Count == 0) continue;

            var tipo = status is >= 200 and < 300 ? Json : ProblemJson;
            if (!respuesta.Content.TryGetValue(tipo, out var media))
                respuesta.Content[tipo] = media = new OpenApiMediaType();
            AsignarEjemplos(media, r.Ejemplos);
        }
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (!docs.Esquemas.TryGetValue(context.JsonTypeInfo.Type, out var doc)) return Task.CompletedTask;

        schema.Description = doc.Descripcion;
        foreach (var (nombre, (descripcion, ejemplo)) in doc.Propiedades)
        {
            if (!schema.Properties.TryGetValue(nombre, out var propiedad)) continue;
            propiedad.Description = descripcion;
            if (ejemplo is not null) propiedad.Example = ComoOpenApi(ejemplo);
        }
        return Task.CompletedTask;
    }

    public static OpenApiResponse RespuestaDeError(string descripcion, object ejemplo) => new()
    {
        Description = descripcion,
        Content = new Dictionary<string, OpenApiMediaType>
        {
            [ProblemJson] = new()
            {
                Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ProblemDetails" } },
                Example = ComoOpenApi(ejemplo)
            }
        }
    };

    private static Dictionary<string, OpenApiMediaType> UnSoloTipo(IDictionary<string, OpenApiMediaType> contenido, string tipo)
    {
        if (contenido.Count == 0) return [];
        var media = contenido.TryGetValue(tipo, out var existente) ? existente : contenido.Values.First();
        return new() { [tipo] = media };
    }

    private static void AsignarEjemplos(OpenApiMediaType media, IReadOnlyList<Ejemplo> ejemplos)
    {
        if (ejemplos.Count == 1)
        {
            media.Example = ComoOpenApi(ejemplos[0].Valor);
            return;
        }
        media.Examples = ejemplos.ToDictionary(
            e => e.Nombre,
            e => new OpenApiExample { Summary = e.Resumen ?? e.Nombre, Value = ComoOpenApi(e.Valor) });
    }

    // Los ejemplos se escriben como objetos C# (DTOs o anónimos) y se serializan igual que las respuestas
    // reales (camelCase), así no pueden quedar desalineados con los nombres del JSON.
    private static IOpenApiAny ComoOpenApi(object valor) =>
        Convertir(JsonNode.Parse(JsonSerializer.Serialize(valor, JsonSerializerOptions.Web)));

    private static IOpenApiAny Convertir(JsonNode? nodo)
    {
        switch (nodo)
        {
            case JsonObject objeto:
                var resultado = new OpenApiObject();
                foreach (var (clave, valor) in objeto) resultado[clave] = Convertir(valor);
                return resultado;
            case JsonArray lista:
                var arreglo = new OpenApiArray();
                arreglo.AddRange(lista.Select(Convertir));
                return arreglo;
            case JsonValue valor when valor.GetValueKind() == JsonValueKind.String:
                return new OpenApiString(valor.GetValue<string>());
            case JsonValue valor when valor.GetValueKind() == JsonValueKind.Number:
                return valor.TryGetValue<long>(out var entero) ? new OpenApiLong(entero) : new OpenApiDouble(valor.GetValue<double>());
            case JsonValue valor when valor.GetValueKind() is JsonValueKind.True or JsonValueKind.False:
                return new OpenApiBoolean(valor.GetValue<bool>());
            default:
                return new OpenApiNull();
        }
    }

    private const string DescripcionGeneral = """
        API del catálogo de **Tejidos Alma**: autenticación con JWT, importación de productos desde
        [Fake Store API](https://fakestoreapi.com/docs) y consulta y edición del catálogo guardado en la base local.

        ## Cómo probar los endpoints

        1. Abre **Auth → Iniciar sesión** (`POST /api/auth/login`) y envíalo con el usuario de demostración:
           `demo@entrevista.local` / `Demo#2026`.
        2. Copia el `accessToken` de la respuesta.
        3. Pégalo en **Authentication → Bearer Token** (sin la palabra `Bearer`).
        4. Ya puedes probar los endpoints marcados con candado. El token dura **15 minutos**; si recibes `401`, vuelve al paso 1.

        ## Autenticación

        | Token | Dónde viaja | Duración |
        |---|---|---|
        | **Access token** (JWT) | En el cuerpo de login/refresh. Se envía como `Authorization: Bearer <token>` | 15 minutos |
        | **Refresh token** | Cookie `HttpOnly` `refresh_token`, solo para `/api/auth` | 7 días; rota en cada uso |

        ## Errores

        **Todos** los errores (validación, autenticación, no encontrado, rutas inexistentes, excepciones…) tienen el mismo
        formato [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`):

        ```json
        {
          "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
          "title": "El producto no existe.",
          "status": 404,
          "detail": "No existe un producto con Id 999.",
          "instance": "/api/products/999",
          "code": "producto_no_encontrado",
          "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"
        }
        ```

        | Campo | Qué es |
        |---|---|
        | `title` | Mensaje en español para mostrar al usuario. |
        | `detail` | Más contexto, cuando lo hay. |
        | `code` | Identificador estable del error. El cliente decide con él, no con el texto. |
        | `traceId` | Identificador de la petición en el log del servidor. |
        | `errors` | Solo en `400`: mensajes por campo, con las claves en camelCase como el JSON. |

        | `code` | Status | Cuándo |
        |---|---|---|
        | `validacion` | 400 | Datos inválidos (ver `errors`) |
        | `no_autenticado` | 401 | Falta el access token, es inválido o venció |
        | `credenciales_invalidas` | 401 | Login con email o contraseña incorrectos |
        | `sesion_expirada` | 401 | Refresh sin cookie válida |
        | `no_encontrado` / `producto_no_encontrado` | 404 | La ruta o el producto no existen |
        | `metodo_no_permitido` | 405 | Método HTTP no soportado en la ruta |
        | `importacion_en_conflicto` | 409 | Dos importaciones a la vez |
        | `tipo_no_soportado` | 415 | El cuerpo no es JSON |
        | `error_interno` | 500 | Error inesperado |
        | `fakestore_no_disponible` | 502 | Fake Store API falló |
        """;
}
