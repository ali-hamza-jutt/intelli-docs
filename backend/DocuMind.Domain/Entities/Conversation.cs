namespace DocuMind.Domain.Entities;

/// <summary>
/// A thread of questions and answers about one document.
///
/// The conversation owns its messages: they are only ever added through it, so a message cannot
/// exist without a thread and the thread's title and last-activity time stay in step with them.
/// </summary>
public class Conversation
{
    /// <summary>Longest title kept. A question longer than this is cut for the rail to read.</summary>
    private const int MaxTitleLength = 80;

    private readonly List<ChatMessage> _messages = [];

    private Conversation() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>
    /// The document every answer in this thread is grounded in. Fixed at creation: changing it
    /// mid-thread would leave earlier citations pointing at a document the thread no longer claims
    /// to be about.
    /// </summary>
    public Guid DocumentId { get; private set; }

    public string Title { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    /// <summary>Last message, not last edit — this is what the conversation list sorts by.</summary>
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    /// <param name="title">Usually the document's name, until the first question replaces it.</param>
    public static Conversation Start(Guid userId, Guid documentId, string title)
    {
        var now = DateTime.UtcNow;

        return new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentId = documentId,
            Title = Shorten(title),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Records a question. The first one becomes the title, which is why the list of conversations
    /// reads like a list of things asked rather than a list of file names.
    /// </summary>
    /// <param name="isFirstTurn">
    /// Whether the thread is empty. Passed in rather than read from <see cref="Messages"/>: a
    /// conversation is loaded without its history when a question is only being appended to it, so
    /// an empty collection here does not mean an empty thread.
    /// </param>
    public ChatMessage AskedBy(string question, bool isFirstTurn)
    {
        if (isFirstTurn)
        {
            Title = Shorten(question);
        }

        return Add(ChatMessage.FromUser(Id, question));
    }

    /// <summary>Records an answer. Sources are attached to the message, not to the thread.</summary>
    /// <param name="stopped">True when the reader cut the answer short, so the text is partial.</param>
    public ChatMessage AnsweredWith(
        string answer,
        bool grounded,
        string? model,
        int inputTokens,
        int outputTokens,
        bool stopped = false)
    {
        return Add(ChatMessage.FromAssistant(
            Id, answer, grounded, model, inputTokens, outputTokens, stopped));
    }

    private ChatMessage Add(ChatMessage message)
    {
        _messages.Add(message);
        UpdatedAt = message.CreatedAt;

        return message;
    }

    private static string Shorten(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.Length == 0)
        {
            return "New conversation";
        }

        return trimmed.Length <= MaxTitleLength
            ? trimmed
            : trimmed[..MaxTitleLength].TrimEnd() + "…";
    }
}
