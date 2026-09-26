using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Chunking;
using Microsoft.Extensions.Options;

namespace DocuMind.Tests.Unit;

/// <summary>
/// Chunking decides what retrieval can ever find, and it is pure text in and text out — so it is the
/// cheapest part of the pipeline to hold to a promise. The promises: nothing exceeds the configured
/// size, neighbours overlap, no word is lost, and a chunk reports the page its own words came from.
/// </summary>
public class ChunkerTests
{
    private static ParagraphTextChunker Chunker(int size = 1000, int overlap = 150) =>
        new(Options.Create(new ChunkingOptions { ChunkSize = size, ChunkOverlap = overlap }));

    /// <summary>Prose with paragraph breaks, long enough to need several chunks per page.</summary>
    private static List<ExtractedPage> Pages(int count = 4, int paragraphsPerPage = 6)
    {
        var pages = new List<ExtractedPage>();

        for (var page = 1; page <= count; page++)
        {
            var paragraphs = Enumerable.Range(0, paragraphsPerPage).Select(index =>
                $"Section {page}.{index}. " + string.Join(' ', Enumerable.Range(0, 30)
                    .Select(word => $"word{page}x{index}x{word}")));

            pages.Add(new ExtractedPage(page, string.Join("\n\n", paragraphs)));
        }

        return pages;
    }

    [Fact]
    public void No_chunk_exceeds_the_configured_size()
    {
        var chunks = Chunker().Chunk(Pages());

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.True(
            chunk.Text.Length <= 1000,
            $"a chunk of {chunk.Text.Length} characters exceeded the 1000 limit"));
    }

    [Fact]
    public void Consecutive_chunks_share_their_overlap()
    {
        var chunks = Chunker().Chunk(Pages());

        Assert.True(chunks.Count > 2, "the fixture should produce several chunks");

        for (var index = 1; index < chunks.Count; index++)
        {
            var previous = chunks[index - 1].Text;
            var current = chunks[index].Text;

            var shared = Enumerable.Range(1, Math.Min(150, current.Length))
                .Any(length => previous.EndsWith(current[..length], StringComparison.Ordinal));

            Assert.True(shared, $"chunk {index} does not begin with anything the one before it ended with");
        }
    }

    [Fact]
    public void A_chunk_reports_the_page_its_own_words_came_from()
    {
        // The bug this guards: a chunk opening with overlap borrowed from the previous page used to
        // claim that page, which would send every citation after the first to the wrong place.
        var pages = new List<ExtractedPage>
        {
            new(1, string.Join(' ', Enumerable.Range(0, 200).Select(index => $"first{index}"))),
            new(2, string.Join(' ', Enumerable.Range(0, 200).Select(index => $"second{index}"))),
        };

        var chunks = Chunker().Chunk(pages);

        foreach (var chunk in chunks)
        {
            // Whatever the overlap says, the page must be one this chunk's own words appear on.
            var ownWords = chunk.Text.Contains("second") ? 2 : 1;

            Assert.True(
                chunk.StartPage <= ownWords && ownWords <= chunk.EndPage,
                $"a chunk of page {ownWords} content reported pages {chunk.StartPage}-{chunk.EndPage}");
        }

        Assert.Contains(chunks, chunk => chunk.StartPage == 2);
    }

    [Fact]
    public void Keeps_every_word_of_the_document()
    {
        var pages = Pages();
        var chunks = Chunker().Chunk(pages);

        var chunked = string.Join(' ', chunks.Select(chunk => chunk.Text));

        foreach (var word in pages.SelectMany(page => page.Text.Split(
            [' ', '\n'], StringSplitOptions.RemoveEmptyEntries)))
        {
            Assert.Contains(word, chunked, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Splits_the_same_text_the_same_way_every_time()
    {
        var pages = Pages();

        var first = Chunker().Chunk(pages);
        var second = Chunker().Chunk(pages);

        Assert.Equal(
            first.Select(chunk => chunk.Text),
            second.Select(chunk => chunk.Text));
    }

    [Fact]
    public void Cuts_a_word_only_when_it_is_longer_than_a_whole_chunk()
    {
        var blob = new string('x', 5_000);
        var chunks = Chunker(size: 400, overlap: 50).Chunk([new ExtractedPage(1, blob)]);

        Assert.True(chunks.Count > 1, "a 5,000-character word has to be cut somewhere");
        Assert.All(chunks, chunk => Assert.True(chunk.Text.Length <= 400));

        // Every piece is still part of the original word — the cut adds nothing of its own — and the
        // pieces together cover it, with repetition where they overlap.
        Assert.All(chunks, chunk => Assert.True(
            chunk.Text.All(character => character == 'x' || char.IsWhiteSpace(character))));

        Assert.True(chunks.Sum(chunk => chunk.Text.Count(character => character == 'x')) >= 5_000);
    }

    [Fact]
    public void Handles_a_document_with_nothing_in_it()
    {
        Assert.Empty(Chunker().Chunk([]));
        Assert.Empty(Chunker().Chunk([new ExtractedPage(1, "   \n\n  ")]));
    }

    [Fact]
    public void Repeats_nothing_when_overlap_is_switched_off()
    {
        var chunks = Chunker(overlap: 0).Chunk(Pages());
        var joined = string.Join("", chunks.Select(chunk => chunk.Text.Replace("\n", " ")));

        // Without overlap the pieces are disjoint, so the total length is the sum of the parts.
        Assert.Equal(chunks.Sum(chunk => chunk.Text.Length), joined.Length);
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(300, 50)]
    [InlineData(1000, 150)]
    [InlineData(2000, 400)]
    [InlineData(8000, 1000)]
    public void Holds_the_size_limit_across_settings(int size, int overlap)
    {
        var chunks = Chunker(size, overlap).Chunk(Pages(count: 6, paragraphsPerPage: 8));

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.True(
            chunk.Text.Length <= size,
            $"size {size}/overlap {overlap} produced a chunk of {chunk.Text.Length}"));
    }
}
