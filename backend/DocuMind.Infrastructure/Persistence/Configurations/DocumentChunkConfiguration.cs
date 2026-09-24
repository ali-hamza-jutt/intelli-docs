using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).IsRequired();

        // Deleting a document removes its chunks, and — once embeddings are added in a later
        // module — their vectors with them. No orphaned vector can outlive its source.
        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // One chunk per position per document. A reprocess that forgot to clear the previous run
        // fails loudly here instead of silently doubling every chunk.
        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();
    }
}
