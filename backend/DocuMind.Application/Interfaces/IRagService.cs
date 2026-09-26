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
    /// <summary>
    /// How many earlier turns may be fed back in. Bounded because history costs tokens on every
    /// question and adds nothing once it is far enough back.
    /// </summary>
    int MaxHistoryMessages { get; }

    /// <param name="userId">Whose documents may be searched. Never taken from client input.</param>
    /// <param name="documentId">Restricts retrieval to one document when set.</param>
    /// <param name="history">
    /// Earlier turns in reading order, oldest first. Used twice: to make sense of a follow-up such
    /// as "and for sick leave?", which means nothing on its own, and to keep the answer consistent
    /// with what was already said.
    /// </param>
    Task<RagAnswer> AskAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same answer, delivered as it is written: pieces of text first, then one final event with
    /// the citations and usage.
    ///
    /// Citations come last on purpose. They are the passages the answer <em>cites</em>, and which
    /// markers it used is not known until the text is finished.
    /// </summary>
    IAsyncEnumerable<RagStreamEvent> StreamAsync(
        Guid userId,
        string question,
        Guid? documentId = null,
        IReadOnlyList<RagTurn>? history = null,
        CancellationToken cancellationToken = default);
}

/// <summary>What a streamed answer emits, in order: any number of deltas, then one final event.</summary>
public abstract record RagStreamEvent
{
    /// <summary>The next piece of the answer's text.</summary>
    public sealed record Delta(string Text) : RagStreamEvent;

    /// <summary>
    /// The finished answer, with its citations and cost. Absent if the stream was cancelled, in
    /// which case the caller keeps whatever text it has already been handed.
    /// </summary>
    public sealed record Final(RagAnswer Answer) : RagStreamEvent;
}

/// <summary>An earlier turn, reduced to what a prompt needs: who said it and what they said.</summary>
public record RagTurn(bool FromUser, string Text);

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
