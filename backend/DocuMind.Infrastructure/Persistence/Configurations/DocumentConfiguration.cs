using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(120);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every document query filters by owner and orders by newest first.
        builder.HasIndex(d => new { d.UserId, d.CreatedAt });
    }
}
