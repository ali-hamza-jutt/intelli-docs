using System.Text;
using System.Text.RegularExpressions;
using DocuMind.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Chunking;

/// <summary>
/// Splits text into overlapping chunks, preferring natural break points.
///
/// Works in two passes. First the text is cut into <em>segments</em>: paragraphs where they fit,
/// sentences where a paragraph is too long, words where a sentence is too long, and a hard cut
/// only as a last resort. Then segments are packed into chunks up to the size limit, and the tail
/// of each chunk is repeated at the start of the next.
///
/// The segment limit is <c>ChunkSize − ChunkOverlap − separator</c>. That arithmetic is what
/// guarantees no chunk ever exceeds <c>ChunkSize</c>: the worst case is a full-length overlap
/// followed by a full-length segment, and those two together still fit.
/// </summary>
public partial class ParagraphTextChunker : ITextChunker
{
    /// <summary>Joins segments inside a chunk. A blank line keeps paragraph structure readable.</summary>
    private const string Separator = "\n\n";

    private readonly ChunkingOptions _options;

    public ParagraphTextChunker(IOptions<ChunkingOptions> options)
    {
        _options = options.Value;
    }

    public int ChunkSize => _options.ChunkSize;

    public int ChunkOverlap => _options.ChunkOverlap;

    public IReadOnlyList<TextChunk> Chunk(IReadOnlyList<ExtractedPage> pages)
    {
        var maxSegment = ChunkSize - ChunkOverlap - Separator.Length;
        var segments = BuildSegments(pages, maxSegment);

        return Pack(segments);
    }

    // ------------------------------------------------------------------ pass one: segments

    private static List<Segment> BuildSegments(IReadOnlyList<ExtractedPage> pages, int maxSegment)
    {
        var segments = new List<Segment>();

        foreach (var page in pages)
        {
            foreach (var paragraph in BlankLine().Split(page.Text))
            {
                var text = paragraph.Trim();

                if (text.Length == 0)
                {
                    continue;
                }

                foreach (var piece in SplitToFit(text, maxSegment))
                {
                    segments.Add(new Segment(piece, page.Number));
                }
            }
        }

        return segments;
    }

    /// <summary>
    /// Returns the paragraph whole if it fits; otherwise packs its sentences, and falls back to
    /// splitting on words for any single sentence that is itself too long.
    /// </summary>
    private static IEnumerable<string> SplitToFit(string paragraph, int max)
    {
        if (paragraph.Length <= max)
        {
            yield return paragraph;
            yield break;
        }

        var buffer = new StringBuilder();

        foreach (var raw in SentenceBoundary().Split(paragraph))
        {
            var sentence = raw.Trim();

            if (sentence.Length == 0)
            {
                continue;
            }

            if (sentence.Length > max)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                foreach (var piece in SplitOnWords(sentence, max))
                {
                    yield return piece;
                }

                continue;
            }

            var needed = buffer.Length == 0 ? sentence.Length : buffer.Length + 1 + sentence.Length;

            if (needed > max)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }

            if (buffer.Length > 0)
            {
                buffer.Append(' ');
            }

            buffer.Append(sentence);
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }

    /// <summary>
    /// Cuts at the last space inside each window. Only text with no spaces at all — a long URL, a
    /// base64 blob — is ever cut mid-word.
    /// </summary>
    private static IEnumerable<string> SplitOnWords(string text, int max)
    {
        var start = 0;

        while (start < text.Length)
        {
            if (text.Length - start <= max)
            {
                yield return text[start..].Trim();
                yield break;
            }

            // Search backwards across [start + 1, start + max] so the piece is never longer than max.
            var cut = text.LastIndexOf(' ', start + max, max);

            if (cut <= start)
            {
                cut = start + max;
            }

            yield return text[start..cut].Trim();

            start = cut;

            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }
        }
    }

    // ------------------------------------------------------------------ pass two: packing

    private List<TextChunk> Pack(List<Segment> segments)
    {
        var chunks = new List<TextChunk>();
        var text = new StringBuilder();
        var startPage = 0;
        var endPage = 0;
        string? carry = null;

        foreach (var segment in segments)
        {
            var projected = text.Length == 0
                ? segment.Text.Length
                : text.Length + Separator.Length + segment.Text.Length;

            if (text.Length > 0 && projected > ChunkSize)
            {
                var emitted = text.ToString();
                chunks.Add(new TextChunk(emitted, startPage, endPage));

                carry = ChunkOverlap > 0 ? Tail(emitted, ChunkOverlap) : null;
                text.Clear();
            }

            if (text.Length == 0)
            {
                if (!string.IsNullOrEmpty(carry))
                {
                    text.Append(carry);
                }

                // The page range comes from the chunk's own segments, never from the overlap.
                // Overlap is context borrowed from the previous chunk; counting it would make
                // almost every chunk claim to start a page early, and a citation that points at
                // PageNumber would land on the wrong page.
                startPage = segment.Page;
                carry = null;
            }

            if (text.Length > 0)
            {
                text.Append(Separator);
            }

            text.Append(segment.Text);
            endPage = segment.Page;
        }

        if (text.Length > 0)
        {
            chunks.Add(new TextChunk(text.ToString(), startPage, endPage));
        }

        return chunks;
    }

    /// <summary>
    /// The last <paramref name="length"/> characters, trimmed forward so the next chunk opens on a
    /// clean boundary: the start of a sentence if one falls in the first half of the window, which
    /// keeps the overlap readable, otherwise the start of a word.
    /// </summary>
    private static string Tail(string text, int length)
    {
        if (text.Length <= length)
        {
            return text;
        }

        var window = text[^length..];

        var sentence = SentenceBoundary().Match(window);

        if (sentence.Success && sentence.Index < length / 2)
        {
            return window[(sentence.Index + sentence.Length)..].Trim();
        }

        var space = window.IndexOfAny([' ', '\n']);

        return (space >= 0 ? window[(space + 1)..] : window).Trim();
    }

    private sealed record Segment(string Text, int Page);

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex BlankLine();

    /// <summary>Whitespace following a sentence terminator. Splits "one. Two" into "one." and "Two".</summary>
    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();
}
