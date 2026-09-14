namespace DocuMind.Domain.Entities;

/// <summary>
/// One page of extracted text. Page numbers captured here are what let a citation say "page 7"
/// several modules from now — the chunker carries them forward, and they cannot be recovered
/// once the pages are flattened into a single string.
/// </summary>
public class DocumentPage
{
    private DocumentPage() { }

    public Guid Id { get; private set; }

    public Guid DocumentTextId { get; private set; }

    /// <summary>1-based, matching what a reader sees in a PDF viewer.</summary>
    public int PageNumber { get; private set; }

    public string Text { get; private set; } = null!;

    public int CharacterCount { get; private set; }

    public int WordCount { get; private set; }

    internal static DocumentPage Create(Guid documentTextId, int pageNumber, string text)
    {
        return new DocumentPage
        {
            Id = Guid.NewGuid(),
            DocumentTextId = documentTextId,
            PageNumber = pageNumber,
            Text = text,
            CharacterCount = text.Length,
            WordCount = TextStatistics.CountWords(text)
        };
    }
}
