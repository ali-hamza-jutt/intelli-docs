using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly DocuMindDbContext _context;

    public ConversationRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
    }

    public async Task<List<ConversationSummary>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Select(conversation => new ConversationSummary(
                conversation.Id,
                conversation.DocumentId,
                conversation.Title,
                conversation.Messages.Count,
                conversation.CreatedAt,
                conversation.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Conversation?> GetWithMessagesAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Messages.OrderBy(message => message.CreatedAt))
                .ThenInclude(message => message.Sources.OrderBy(source => source.Marker))
            .FirstOrDefaultAsync(
                conversation => conversation.Id == id && conversation.UserId == userId,
                cancellationToken);
    }

    public async Task<Conversation?> GetAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Tracked: the caller appends messages to it and saves.
        return await _context.Conversations
            .FirstOrDefaultAsync(
                conversation => conversation.Id == id && conversation.UserId == userId,
                cancellationToken);
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Newest first to take the tail of the thread, then flipped back into reading order.
        var recent = await _context.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        recent.Reverse();

        return recent;
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        // The message's sources come with it: they are reachable only from it and have no keys yet.
        await _context.ChatMessages.AddAsync(message);
    }

    public Task RemoveAsync(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
