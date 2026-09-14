namespace DocuMind.Application.Interfaces;

/// <summary>
/// Runs the ingestion pipeline for one document: fetch the file, extract text, clean it, store it,
/// and move the document's status along.
///
/// Module 4 moves the call onto a background worker; nothing about this contract changes when it
/// does, which is why the orchestration lives behind an interface from the start.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Processes a document by id. Never throws for content problems — a document that cannot be
    /// read is marked Failed with a message the user can act on.
    /// </summary>
    Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
}
