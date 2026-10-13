namespace Entrevista.Api.Data.Seed;

public class FakeStoreClient(HttpClient http)
{
    public async Task<List<string>> GetCategoriasAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<string>>("products/categories", ct) ?? [];

    public async Task<List<FakeStoreProductDto>> GetProductosAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<FakeStoreProductDto>>("products", ct) ?? [];
}
