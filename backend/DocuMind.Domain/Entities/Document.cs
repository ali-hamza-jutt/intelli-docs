namespace DocuMind.Domain.Entities;

public class Document
{
    public Guid Id { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long FileSize { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Document(
        string fileName,
        string contentType,
        long fileSize)
    {
        Id = Guid.NewGuid();
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        CreatedAt = DateTime.UtcNow;
    }
}