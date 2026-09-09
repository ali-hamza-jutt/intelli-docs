namespace DocuMind.Domain.Entities;

public class Document
{
    private Document() { }

    public Guid Id { get; private set; }

    /// <summary>Owner. Every read of a document must be filtered by this.</summary>
    public Guid UserId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long FileSize { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Document(
        Guid userId,
        string fileName,
        string contentType,
        long fileSize)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        CreatedAt = DateTime.UtcNow;
    }
}
