using System.Text.Json;
using Entrevista.Api.Modules.Catalog.DTOs;

namespace Entrevista.Api.Modules.Catalog.Services;

// Error al comunicarse con FakeStore API (sin conexión, timeout, status no exitoso o JSON inválido).
public class FakeStoreException(string message, Exception? inner = null) : Exception(message, inner);

public class FakeStoreClient(HttpClient http)
{
    public Task<List<string?>> GetCategoriasAsync(CancellationToken ct = default) =>
        GetListAsync<string?>("products/categories", ct);

    public Task<List<FakeStoreProductDto?>> GetProductosAsync(CancellationToken ct = default) =>
        GetListAsync<FakeStoreProductDto?>("products", ct);

    private async Task<List<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
                throw new FakeStoreException(
                    $"FakeStore API respondió {(int)response.StatusCode} ({response.ReasonPhrase}) en /{path}.");

            return await response.Content.ReadFromJsonAsync<List<T>>(ct)
                ?? throw new FakeStoreException($"FakeStore API devolvió una respuesta vacía en /{path}.");
        }
        catch (HttpRequestException ex)
        {
            throw new FakeStoreException($"No se pudo conectar con FakeStore API (/{path}).", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new FakeStoreException($"FakeStore API no respondió a tiempo (/{path}).", ex);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new FakeStoreException($"FakeStore API devolvió datos con formato inválido en /{path}.", ex);
        }
    }
}
