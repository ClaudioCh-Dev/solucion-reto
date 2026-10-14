using System.Text;
using Entrevista.Api.Common;
using Entrevista.Api.Common.Errores;
using Entrevista.Api.Common.OpenApi;
using Entrevista.Api.Data;
using Entrevista.Api.Modules.Auth;
using Entrevista.Api.Modules.Auth.Entities;
using Entrevista.Api.Modules.Auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Entrevista.Api.Modules.Catalog.Services;
using Entrevista.Api.Modules.Categorias.Services;
using Entrevista.Api.Modules.Productos.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        $"Falta ConnectionStrings:DefaultConnection para el entorno '{builder.Environment.EnvironmentName}'.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpClient<FakeStoreClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FakeStore:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ImportacionCatalogoService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<CategoriaService>();

// JWT: si falta la configuración o la clave es corta, la app no arranca.
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>();
if (jwt is null || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
    throw new InvalidOperationException(
        $"Falta la sección Jwt (Issuer, Audience, Key) para el entorno '{builder.Environment.EnvironmentName}'.");
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
if (jwt.AccessTokenMinutos <= 0 || jwt.RefreshTokenDias <= 0)
    throw new InvalidOperationException("Jwt:AccessTokenMinutos y Jwt:RefreshTokenDias deben ser mayores que 0.");
if (jwt.RefreshCookie.SameSite == SameSiteMode.None && !jwt.RefreshCookie.Secure)
    throw new InvalidOperationException("Jwt:RefreshCookie: SameSite=None exige Secure=true (los navegadores la rechazan).");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Autenticación por defecto
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Mantiene los nombres originales de los claims ("sub", "email")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Por defecto acepta 5 min extra tras vencer; así vence a la hora exacta
        };
    });
builder.Services.AddAuthorization();

// Prefijo global: todas las rutas de los controladores quedan bajo /api.
builder.Services.AddControllers(options =>
        options.Conventions.Add(new RoutePrefixConvention("api")))
    // Errores 400 de validación con mensajes en español (incluye JSON con formato inválido).
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = ValidacionResponseFactory.Crear);

// Todos los errores con el mismo formato ProblemDetails (Common/Errores).
builder.Services.AddErroresEstandar();

// Documento OpenAPI con el esquema Bearer, textos y ejemplos de cada módulo (Modules/*/OpenApi).
builder.Services.AddDocumentacionApi();

var app = builder.Build();

app.UseErroresEstandar(); // Primero: así también da formato a los errores de los middlewares siguientes

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapDocumentacionApi(); // /openapi/v1.json y /scalar

    // En dev: aplica migraciones, crea el usuario de demostración e importa categorías y productos de FakeStore API.
    // Si la API falla, la app arranca igual; se puede reintentar con POST api/catalog/import.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    var demoEmail = app.Configuration["UsuarioDemo:Email"];
    var demoPassword = app.Configuration["UsuarioDemo:Password"];
    if (!string.IsNullOrWhiteSpace(demoEmail) && !string.IsNullOrWhiteSpace(demoPassword))
        await scope.ServiceProvider.GetRequiredService<AuthService>().CrearUsuarioSiNoExisteAsync(demoEmail, demoPassword);

    try
    {
        await scope.ServiceProvider.GetRequiredService<ImportacionCatalogoService>().ImportarAsync();
    }
    catch (FakeStoreException ex)
    {
        app.Logger.LogWarning("No se pudo importar desde FakeStore al arrancar: {Mensaje}", ex.Message);
    }
}

// Redirige HTTP a HTTPS. Se desactiva con "ForzarHttps": false (en dev se usa solo HTTP).
if (app.Configuration.GetValue("ForzarHttps", true))
    app.UseHttpsRedirection();

app.UseAuthentication(); // Lee el JWT del header Authorization y carga el usuario
app.UseAuthorization(); // Aplica [Authorize]: sin token válido responde 401

app.MapControllers(); // Mapea los controladores

app.Run(); // Inicia el servidor HTTP
