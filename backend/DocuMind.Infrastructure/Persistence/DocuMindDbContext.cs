using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class DocuMindDbContext : DbContext
{
    public DocuMindDbContext(DbContextOptions<DocuMindDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentText> DocumentTexts => Set<DocumentText>();

    public DbSet<DocumentPage> DocumentPages => Set<DocumentPage>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Declared on the model so the migration installs it. Vector columns, the <=> operator and
        // the HNSW index all come from this extension.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuMindDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
