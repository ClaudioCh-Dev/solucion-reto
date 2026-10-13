namespace Entrevista.Api.Data.Seed;

public record FakeStoreProductDto(
    int Id,
    string Title,
    decimal Price,
    string Description,
    string Category,
    string Image);
