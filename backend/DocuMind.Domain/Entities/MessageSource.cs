namespace DocuMind.Domain.Entities;

/// <summary>
/// A passage an answer was written from, snapshotted onto the answer.
///
/// Every field here is a copy, including the document's name and the passage text, and
/// <see cref="DocumentId"/> is deliberately not a foreign key. A citation is a record of what the
/// answer was based on at the time it was written; it has to survive the document being renamed,
/// reprocessed into different chunks, or deleted outright.
/// </summary>
public class MessageSource
{
    private MessageSource() { }

    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    /// <summary>The [n] this passage was cited as, matching the marker in the answer's text.</summary>
    public int Marker { get; private set; }

    /// <summary>Which document it came from, for a link that may no longer resolve.</summary>
    public Guid DocumentId { get; private set; }

    public string FileName { get; private set; } = null!;

    public int PageNumber { get; private set; }

    public int EndPageNumber { get; private set; }

    public string Text { get; private set; } = null!;

    /// <summary>How close the passage was to the question, 0 to 1.</summary>
    public double Similarity { get; private set; }

    internal static MessageSource Create(
        int marker,
        Guid documentId,
        string fileName,
        int pageNumber,
        int endPageNumber,
        string text,
        double similarity)
    {
        if (pageNumber < 1 || endPageNumber < pageNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endPageNumber), $"Page range {pageNumber}–{endPageNumber} is not valid.");
        }

        // Id and MessageId are left unset: both are filled in when the source is saved with the
        // answer it belongs to, and a pre-filled key would be read as a row that already exists.
        return new MessageSource
        {
            Marker = marker,
            DocumentId = documentId,
            FileName = fileName,
            PageNumber = pageNumber,
            EndPageNumber = endPageNumber,
            Text = text,
            Similarity = similarity
        };
    }
}
