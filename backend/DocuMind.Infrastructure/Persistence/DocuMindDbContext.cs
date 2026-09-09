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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocuMindDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
