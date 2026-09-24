namespace DocuMind.Application.Interfaces;

/// <summary>
/// Retrieval-augmented generation: find the passages that bear on a question, put them in front of
/// a model, and return what it says together with the passages it used.
///
/// The whole point is that the answer is traceable. Anything the model asserts should be checkable
/// against a citation, and a question the documents cannot answer should produce a refusal rather
/// than a fluent invention.
/// </summary>
public interface IRagService
{
    /// <param name="userId">Whose documents may be searched. Never taken from client input.</param>
    /// <param name="documentId">Restricts retrieval to one document when set.</param>
    Task<RagAnswer> AskAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// An answer and its evidence.
/// </summary>
/// <param name="Grounded">
/// False when nothing relevant was found and the model was never asked. The distinction matters:
/// a caller can tell "your documents do not cover this" from "the model declined".
/// </param>
public record RagAnswer(
    string Question,
    string Answer,
    bool Grounded,
    IReadOnlyList<RagCitation> Citations,
    string? Model,
    int InputTokens,
    int OutputTokens);

/// <summary>
/// A cited passage, copied rather than referenced: the file name, page range and text are snapshots
/// taken when the answer was written, so a citation still reads correctly after the document is
/// renamed, reprocessed into different chunks, or deleted.
/// </summary>
/// <param name="Marker">The number the answer cites, matching the [n] in its text.</param>
public record RagCitation(
    int Marker,
    Guid DocumentId,
    string FileName,
    int PageNumber,
    int EndPageNumber,
    string Text,
    double Similarity);
