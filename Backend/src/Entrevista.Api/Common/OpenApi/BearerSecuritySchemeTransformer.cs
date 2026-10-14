using Entrevista.Api.Common.Errores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Entrevista.Api.Common.OpenApi;

// Declara la autenticación JWT Bearer en el documento OpenAPI y la exige en los endpoints con [Authorize].
// Así Scalar muestra el candado y envía "Authorization: Bearer <token>" al probarlos.
public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    public const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token de `POST /api/auth/login` (dura 15 minutos). Pégalo sin el prefijo `Bearer`."
        };
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiereToken = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (!requiereToken) return Task.CompletedTask;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = Esquema } }] = []
        });

        // El 401 lo responde el middleware de autenticación antes de llegar al controller.
        operation.Responses["401"] = DocumentacionTransformer.RespuestaDeError(
            "Falta el access token, es inválido o venció. Incluye el header `WWW-Authenticate: Bearer`.",
            EjemplosComunes.Problema(ErroresApi.NoAutenticado, "/" + context.Description.RelativePath));
        return Task.CompletedTask;
    }
}
