using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class UsageRecordConfiguration : IEntityTypeConfiguration<UsageRecord>
{
    public void Configure(EntityTypeBuilder<UsageRecord> builder)
    {
        builder.HasKey(record => record.Id);

        builder.Property(record => record.Model).IsRequired().HasMaxLength(100);

        // Stored as the name so a reordered enum cannot reassign history to the wrong kind of call.
        builder.Property(record => record.Kind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Closing an account takes its usage history with it.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every read is "this user, since this date", which is exactly this index.
        builder.HasIndex(record => new { record.UserId, record.CreatedAt });
    }
}
