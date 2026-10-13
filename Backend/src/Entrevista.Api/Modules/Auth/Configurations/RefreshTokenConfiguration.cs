using Entrevista.Api.Modules.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entrevista.Api.Modules.Auth.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);

        // SHA-256 en hexadecimal = 64 caracteres.
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.Property(r => r.ReemplazadoPorHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);

        builder.HasOne(r => r.Usuario)
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borran sus tokens
    }
}
