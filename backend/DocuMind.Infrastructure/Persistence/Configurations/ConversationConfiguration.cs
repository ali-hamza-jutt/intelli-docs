using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocuMind.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation => conversation.Title).IsRequired().HasMaxLength(120);

        // Messages are reached only through the conversation, so the navigation is mapped to the
        // backing field rather than a public setter.
        builder.Metadata
            .FindNavigation(nameof(Conversation.Messages))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting an account takes its conversations with it.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // No foreign key to the document, for the same reason a citation has none: the thread is a
        // record of what was asked and answered, and deleting the file should not erase it. Such a
        // thread can still be read — its stored citations keep their text — but a new question in it
        // has nothing left to retrieve, so it answers that it found nothing.
        builder.HasIndex(conversation => conversation.DocumentId);

        // The conversation list is "mine, most recent first" — this is the index that serves it.
        builder.HasIndex(conversation => new { conversation.UserId, conversation.UpdatedAt })
            .IsDescending(false, true);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Content).IsRequired();

        // Stored as the name, not the number: a migration that reorders the enum cannot silently
        // turn every question into an answer.
        builder.Property(message => message.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(message => message.Model).HasMaxLength(100);

        builder.Metadata
            .FindNavigation(nameof(ChatMessage.Sources))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(message => message.Sources)
            .WithOne()
            .HasForeignKey(source => source.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
    }
}

public class MessageSourceConfiguration : IEntityTypeConfiguration<MessageSource>
{
    public void Configure(EntityTypeBuilder<MessageSource> builder)
    {
        builder.HasKey(source => source.Id);

        builder.Property(source => source.FileName).IsRequired().HasMaxLength(260);

        builder.Property(source => source.Text).IsRequired();

        // No foreign key on DocumentId, on purpose. A citation is a snapshot of what an answer was
        // based on; a constraint here would delete history when a document is deleted, which is the
        // opposite of what a citation is for.
        builder.HasIndex(source => source.MessageId);
    }
}
