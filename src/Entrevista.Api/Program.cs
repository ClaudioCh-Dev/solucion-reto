using Entrevista.Api.Data;
using Entrevista.Api.Modules.Productos.Services;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddScoped<ImportacionProductosService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // En dev: aplica migraciones e importa productos de FakeStore API.
    // Si la API falla, la app arranca igual; se puede reintentar con POST api/productos/importar.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    try
    {
        await scope.ServiceProvider.GetRequiredService<ImportacionProductosService>().ImportarAsync();
    }
    catch (FakeStoreException ex)
    {
        app.Logger.LogWarning("No se pudo importar desde FakeStore al arrancar: {Mensaje}", ex.Message);
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
