using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).IsRequired();

        // The Domain holds a plain float[]; pgvector's own type appears only here, on the way to
        // and from the column. Width is fixed in the column type, so a vector of the wrong size is
        // rejected by the database as well as by the entity.
        builder.Property(c => c.Embedding)
            .HasColumnType($"vector({DocumentChunk.EmbeddingDimensions})")
            // Nullable on both sides of the converter because the column is: EF skips the
            // conversion entirely for a chunk that has not been embedded yet. The comparer is what
            // makes an array compare by contents — without it EF compares references and would miss
            // a re-embedded chunk.
            .HasConversion(
                new ValueConverter<float[]?, Vector>(
                    value => new Vector(value!),
                    stored => stored.ToArray()),
                new ValueComparer<float[]?>(
                    (left, right) => left == null
                        ? right == null
                        : right != null && left.SequenceEqual(right),
                    value => value == null
                        ? 0
                        : value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    value => value == null ? null : value.ToArray()));

        // Deleting a document removes its chunks and their vectors with them. No orphaned vector
        // can outlive its source.
        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // One chunk per position per document. A reprocess that forgot to clear the previous run
        // fails loudly here instead of silently doubling every chunk.
        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();

        // HNSW turns "nearest vectors" from a scan of every chunk into a graph walk. The operator
        // class must match the operator the query uses: cosine distance, <=>. A query ordering by a
        // different operator would silently ignore this index.
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
