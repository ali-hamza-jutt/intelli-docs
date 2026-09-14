using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentTextConfiguration : IEntityTypeConfiguration<DocumentText>
{
    public void Configure(EntityTypeBuilder<DocumentText> builder)
    {
        builder.HasKey(t => t.Id);

        // No length cap: this is the whole document body, and Postgres text is unbounded.
        builder.Property(t => t.Text).IsRequired();

        // One extraction per document — a second row would mean two answers to "what does this
        // document say", which is exactly the ambiguity the reprocess path must avoid.
        builder.HasIndex(t => t.DocumentId).IsUnique();

        builder.HasOne<Document>()
            .WithOne()
            .HasForeignKey<DocumentText>(t => t.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Pages)
            .WithOne()
            .HasForeignKey(p => p.DocumentTextId)
            .OnDelete(DeleteBehavior.Cascade);

        // The aggregate exposes pages read-only, so EF must go through the backing field.
        builder.Metadata
            .FindNavigation(nameof(DocumentText.Pages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class DocumentPageConfiguration : IEntityTypeConfiguration<DocumentPage>
{
    public void Configure(EntityTypeBuilder<DocumentPage> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Text).IsRequired();

        // Pages are always read in order, for one extraction at a time.
        builder.HasIndex(p => new { p.DocumentTextId, p.PageNumber }).IsUnique();
    }
}
