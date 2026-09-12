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

        builder.Property(d => d.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(d => d.StoredFileName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(120);

        // Stored as an int so the column stays compact; the enum order is fixed as a result.
        builder.Property(d => d.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(1000);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every document query filters by owner and orders by newest first.
        builder.HasIndex(d => new { d.UserId, d.CreatedAt });

        // The background worker in module 4 will scan for pending work by status.
        builder.HasIndex(d => d.Status);
    }
}
