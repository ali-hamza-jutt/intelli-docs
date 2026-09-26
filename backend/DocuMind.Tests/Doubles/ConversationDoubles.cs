using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Tests.Doubles;

/// <summary>
/// Conversations held in a list, so the rules around them can be tested without a database.
///
/// Adding and saving are kept apart, as they are in the real repository: a message that was added
/// but never saved was never written anywhere, and a test that cannot tell the difference would
/// miss the case where an interrupted answer leaves a question stranded.
/// </summary>
public class FakeConversationRepository : IConversationRepository
{
    private readonly List<Conversation> _conversations = [];
    private readonly List<ChatMessage> _pending = [];

    /// <summary>Only what a save actually committed.</summary>
    public List<ChatMessage> Messages { get; } = [];

    public int Saves { get; private set; }

    public Conversation Add(Guid userId, Guid documentId, string title = "handbook.pdf")
    {
        var conversation = Conversation.Start(userId, documentId, title);
        _conversations.Add(conversation);

        return conversation;
    }

    public Task AddAsync(Conversation conversation)
    {
        _conversations.Add(conversation);

        return Task.CompletedTask;
    }

    public Task AddMessageAsync(ChatMessage message)
    {
        _pending.Add(message);

        return Task.CompletedTask;
    }

    public Task<Conversation?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_conversations.FirstOrDefault(
            conversation => conversation.Id == id && conversation.UserId == userId));

    public Task<Conversation?> GetWithMessagesAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        GetAsync(id, userId, cancellationToken);

    public Task<List<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Messages
            .Where(message => message.ConversationId == conversationId)
            .TakeLast(take)
            .ToList());

    public Task<List<ConversationSummary>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_conversations
            .Where(conversation => conversation.UserId == userId)
            .Select(conversation => new ConversationSummary(
                conversation.Id, conversation.DocumentId, conversation.Title, 0,
                conversation.CreatedAt, conversation.UpdatedAt))
            .ToList());

    public Task RemoveAsync(Conversation conversation)
    {
        _conversations.Remove(conversation);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Messages.AddRange(_pending);
        _pending.Clear();
        Saves++;

        return Task.CompletedTask;
    }
}

/// <summary>Only what the conversation service asks of it.</summary>
public class FakeDocumentRepository : IDocumentRepository
{
    private readonly List<Document> _documents = [];

    public Document Add(Guid userId)
    {
        var document = Document.Upload(
            userId, "handbook.pdf", "stored.pdf", "uploads/stored.pdf", "application/pdf", 1024);

        document.MarkCompleted();
        _documents.Add(document);

        return document;
    }

    public Task AddAsync(Document document)
    {
        _documents.Add(document);

        return Task.CompletedTask;
    }

    public Task<List<Document>> GetAllForUserAsync(Guid userId) =>
        Task.FromResult(_documents.Where(document => document.UserId == userId).ToList());

    public Task<Document?> GetByIdForUserAsync(Guid id, Guid userId) =>
        Task.FromResult(_documents.FirstOrDefault(
            document => document.Id == id && document.UserId == userId));

    public Task<Document?> GetByIdAsync(Guid id) =>
        Task.FromResult(_documents.FirstOrDefault(document => document.Id == id));

    public Task<List<Document>> GetByStatusAsync(IReadOnlyCollection<DocumentStatus> statuses) =>
        Task.FromResult(_documents.Where(document => statuses.Contains(document.Status)).ToList());

    public Task DeleteAsync(Document document)
    {
        _documents.Remove(document);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

/// <summary>A signed-in user, without a request to read one from.</summary>
public class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid userId)
    {
        UserId = userId;
    }

    public Guid? UserId { get; }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId() => UserId ?? throw new InvalidOperationException("No user.");
}

/// <summary>
/// Answers from a script, slowly enough to be interrupted — which is the only way to test what a
/// half-written answer leaves behind.
/// </summary>
public class ScriptedRagService : IRagService
{
    private readonly string[] _words;

    public ScriptedRagService(string answer, TimeSpan? delay = null)
    {
        _words = answer.Split(' ');
        Delay = delay ?? TimeSpan.Zero;
    }

    public TimeSpan Delay { get; set; }

    public int MaxHistoryMessages => 8;

    /// <summary>The history it was handed, for checking that a thread is replayed.</summary>
    public IReadOnlyList<RagTurn>? SeenHistory { get; private set; }

    public Task<RagAnswer> AskAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        CancellationToken cancellationToken = default)
    {
        SeenHistory = history;

        return Task.FromResult(new RagAnswer(
            question, string.Join(' ', _words), Grounded: true, [], "scripted-model", 10, 5));
    }

    public async IAsyncEnumerable<RagStreamEvent> StreamAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        SeenHistory = history;

        for (var index = 0; index < _words.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            yield return new RagStreamEvent.Delta(index == 0 ? _words[index] : $" {_words[index]}");
        }

        yield return new RagStreamEvent.Final(new RagAnswer(
            question, string.Join(' ', _words), Grounded: true, [], "scripted-model", 10, 5));
    }
}
