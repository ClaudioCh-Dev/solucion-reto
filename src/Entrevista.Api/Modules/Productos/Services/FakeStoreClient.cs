using System.Text.Json;
using Entrevista.Api.Modules.Productos.DTOs;

namespace Entrevista.Api.Modules.Productos.Services;

// Error al comunicarse con FakeStore API (sin conexión, timeout, status no exitoso o JSON inválido).
public class FakeStoreException(string message, Exception? inner = null) : Exception(message, inner);

public class FakeStoreClient(HttpClient http)
{
    public async Task<List<FakeStoreProductDto?>> GetProductosAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync("products", ct);
            if (!response.IsSuccessStatusCode)
                throw new FakeStoreException(
                    $"FakeStore API respondió {(int)response.StatusCode} ({response.ReasonPhrase}).");

            return await response.Content.ReadFromJsonAsync<List<FakeStoreProductDto?>>(ct)
                ?? throw new FakeStoreException("FakeStore API devolvió una respuesta vacía.");
        }
        catch (HttpRequestException ex)
        {
            throw new FakeStoreException("No se pudo conectar con FakeStore API.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new FakeStoreException("FakeStore API no respondió a tiempo.", ex);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new FakeStoreException("FakeStore API devolvió datos con formato inválido.", ex);
        }
    }
}
