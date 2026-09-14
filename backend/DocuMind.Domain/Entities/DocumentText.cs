namespace DocuMind.Domain.Entities;

/// <summary>
/// The text extracted from a document, held apart from <see cref="Document"/> so that listing
/// documents never drags megabytes of body text along with it.
/// </summary>
public class DocumentText
{
    private readonly List<DocumentPage> _pages = [];

    private DocumentText() { }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    /// <summary>Every page joined in reading order. Convenient for preview and search.</summary>
    public string Text { get; private set; } = null!;

    public int CharacterCount { get; private set; }

    public int WordCount { get; private set; }

    public int PageCount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<DocumentPage> Pages => _pages.AsReadOnly();

    /// <summary>
    /// Builds the record from per-page text. Pages arrive in reading order and are joined with a
    /// blank line, which keeps paragraph detection working when the chunker splits it later.
    /// </summary>
    public static DocumentText Create(
        Guid documentId,
        IReadOnlyList<(int PageNumber, string Text)> pages)
    {
        if (pages.Count == 0)
        {
            throw new ArgumentException("A document needs at least one page of text.", nameof(pages));
        }

        var combined = string.Join("\n\n", pages.Select(page => page.Text));

        var documentText = new DocumentText
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Text = combined,
            CharacterCount = combined.Length,
            WordCount = TextStatistics.CountWords(combined),
            PageCount = pages.Count,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (pageNumber, text) in pages)
        {
            documentText._pages.Add(DocumentPage.Create(documentText.Id, pageNumber, text));
        }

        return documentText;
    }
}
