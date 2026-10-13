using Entrevista.Api.Modules.Productos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrevista.Api.Modules.Productos.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ExternalId).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.ExternalId).IsUnique();

        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Descripcion).HasMaxLength(2000);
        builder.Property(p => p.Precio).HasPrecision(18, 2);
        builder.Property(p => p.ImagenUrl).HasMaxLength(2048);

        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict); // No permite borrar una categoría que tenga productos
    }
}
