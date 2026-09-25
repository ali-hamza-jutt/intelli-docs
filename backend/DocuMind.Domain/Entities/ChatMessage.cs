namespace DocuMind.Domain.Entities;

/// <summary>Who wrote a message. Assigned by the server; never accepted from a client.</summary>
public enum MessageRole
{
    User = 0,
    Assistant = 1
}

/// <summary>
/// One turn in a conversation.
///
/// An assistant turn carries what it was answered with — the sources, whether anything was found at
/// all, and what it cost — because all of that is only true of that one answer and would be wrong to
/// recompute later against a document that may since have changed.
/// </summary>
public class ChatMessage
{
    private readonly List<MessageSource> _sources = [];

    private ChatMessage() { }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public MessageRole Role { get; private set; }

    public string Content { get; private set; } = null!;

    /// <summary>
    /// For an answer: whether any passage was close enough to answer from. False means the model was
    /// never asked, so the content is a refusal rather than a claim. Null on a question.
    /// </summary>
    public bool? Grounded { get; private set; }

    /// <summary>The model that wrote an answer, or null on a question or an ungrounded refusal.</summary>
    public string? Model { get; private set; }

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<MessageSource> Sources => _sources.AsReadOnly();

    internal static ChatMessage FromUser(Guid conversationId, string question)
    {
        return Create(conversationId, MessageRole.User, question);
    }

    internal static ChatMessage FromAssistant(
        Guid conversationId,
        string answer,
        bool grounded,
        string? model,
        int inputTokens,
        int outputTokens)
    {
        var message = Create(conversationId, MessageRole.Assistant, answer);

        message.Grounded = grounded;
        message.Model = model;
        message.InputTokens = inputTokens;
        message.OutputTokens = outputTokens;

        return message;
    }

    /// <summary>
    /// Attaches the passages this answer cited, as copies. Nothing here is a foreign key into the
    /// document: the point is that the citation still reads correctly if that document is renamed,
    /// re-chunked or deleted.
    /// </summary>
    public void Cite(
        int marker,
        Guid documentId,
        string fileName,
        int pageNumber,
        int endPageNumber,
        string text,
        double similarity)
    {
        if (Role != MessageRole.Assistant)
        {
            throw new InvalidOperationException("Only an answer can cite sources.");
        }

        _sources.Add(MessageSource.Create(
            marker, documentId, fileName, pageNumber, endPageNumber, text, similarity));
    }

    private static ChatMessage Create(Guid conversationId, MessageRole role, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("A message must have content.", nameof(content));
        }

        // Id is left unset on purpose. A turn is usually added to a conversation that has already
        // been saved, and persistence treats a child that arrives with a key already filled in as a
        // row that exists — it would issue an UPDATE that matches nothing instead of an INSERT.
        return new ChatMessage
        {
            ConversationId = conversationId,
            Role = role,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
