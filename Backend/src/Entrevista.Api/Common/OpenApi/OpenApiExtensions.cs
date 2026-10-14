using Scalar.AspNetCore;

namespace Entrevista.Api.Common.OpenApi;

public static class OpenApiExtensions
{
    // Genera el documento OpenAPI con el esquema Bearer y la documentación de cada módulo.
    public static IServiceCollection AddDocumentacionApi(this IServiceCollection services)
    {
        // Busca en el ensamblado todas las clases IDocumentacionModulo (una por módulo) y las ejecuta.
        var docs = new DocumentacionApi();
        var modulos = typeof(OpenApiExtensions).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDocumentacionModulo).IsAssignableFrom(t));
        foreach (var modulo in modulos)
            ((IDocumentacionModulo)Activator.CreateInstance(modulo)!).Documentar(docs);
        services.AddSingleton(docs);

        return services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
            // Después del Bearer, para que los textos de cada módulo tengan la última palabra.
            options.AddDocumentTransformer<DocumentacionTransformer>();
            options.AddOperationTransformer<DocumentacionTransformer>();
            options.AddSchemaTransformer<DocumentacionTransformer>();
        });
    }

    // /openapi/v1.json (documento) y /scalar (interfaz para explorar y probar la API).
    public static WebApplication MapDocumentacionApi(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options => options
            .WithTitle("Tejidos Alma · API")
            .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
            .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.Esquema));
        return app;
    }
}
