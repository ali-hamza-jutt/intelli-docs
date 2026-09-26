using System.Runtime.CompilerServices;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;

namespace DocuMind.Tests.Doubles;

/*
 * Stand-ins for the two services that cost money and the one that records what they cost.
 *
 * Written by hand rather than with a mocking library: each one is a dozen lines, a test reads as
 * plain code rather than as configured expectations, and they double as the fakes the integration
 * tests register in the host.
 */

/// <summary>A model that returns whatever it was told to, and remembers what it was asked.</summary>
public class FakeLlmService : ILLMService
{
    private readonly Queue<string> _answers = new();

    public string Model { get; set; } = "fake-model";

    /// <summary>Every prompt it received, in order — what the prompt rules are asserted against.</summary>
    public List<LlmPrompt> Prompts { get; } = [];

    public int InputTokens { get; set; } = 100;

    public int OutputTokens { get; set; } = 20;

    /// <summary>Thrown instead of answering, for the provider-failure paths.</summary>
    public Exception? Throws { get; set; }

    /// <summary>Pauses between streamed words, so a test can stop a stream part-way.</summary>
    public TimeSpan StreamDelay { get; set; } = TimeSpan.Zero;

    public FakeLlmService WillAnswer(params string[] answers)
    {
        foreach (var answer in answers)
        {
            _answers.Enqueue(answer);
        }

        return this;
    }

    public Task<LlmCompletion> CompleteAsync(
        LlmPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        Prompts.Add(prompt);

        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(new LlmCompletion(NextAnswer(), InputTokens, OutputTokens));
    }

    public async IAsyncEnumerable<LlmChunk> StreamAsync(
        LlmPrompt prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Prompts.Add(prompt);

        if (Throws is not null)
        {
            throw Throws;
        }

        var words = NextAnswer().Split(' ');

        for (var index = 0; index < words.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (StreamDelay > TimeSpan.Zero)
            {
                await Task.Delay(StreamDelay, cancellationToken);
            }

            // Spaces go on the front of every word but the first, so the pieces reassemble exactly.
            yield return new LlmChunk(index == 0 ? words[index] : $" {words[index]}");
        }

        // The last chunk carries the cost and no text, as a real provider's does.
        yield return new LlmChunk(string.Empty, InputTokens, OutputTokens);
    }

    private string NextAnswer() => _answers.Count > 0 ? _answers.Dequeue() : "A fake answer [1].";
}

/// <summary>
/// Deterministic vectors: the same text always gives the same one, and two texts that share words
/// land closer together than two that do not. Enough for the parts of the app that only care that
/// retrieval is ordered and repeatable.
/// </summary>
public class FakeEmbeddingService : IEmbeddingService
{
    public string Model => "fake-embedding";

    public int Dimensions => DocumentChunk.EmbeddingDimensions;

    public int Calls { get; private set; }

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(Vector(text));
    }

    public Task<EmbeddingBatch> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(new EmbeddingBatch(
            [.. texts.Select(Vector)],
            texts.Sum(text => text.Length / 4)));
    }

    /// <summary>A hashed bag of words, normalised — crude, but shared words really do bring two
    /// texts closer, which is the only property the tests rely on.</summary>
    private float[] Vector(string text)
    {
        var vector = new float[Dimensions];

        foreach (var word in text.ToLowerInvariant().Split(
            [' ', '\n', '\r', '\t', '.', ',', '?', '!', ':', ';'],
            StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length <= 2)
            {
                continue;
            }

            vector[(uint)word.GetDeterministicHash() % Dimensions] += 1;
        }

        var length = Math.Sqrt(vector.Sum(value => value * value));

        if (length > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] = (float)(vector[i] / length);
            }
        }

        return vector;
    }
}

/// <summary>
/// Retrieval, without a database. A test hands it the passages it should find, which is what lets
/// the RAG rules be tested apart from whether the SQL ranks correctly — that is the repository's
/// job, and it has its own test against a real Postgres.
/// </summary>
public class FakeVectorSearchService : IVectorSearchService
{
    private readonly List<ChunkMatch> _matches = [];

    public double SimilarityThreshold { get; set; } = 0.5;

    /// <summary>Every query it was asked, which is how "the follow-up carried context" is checked.</summary>
    public List<string> Queries { get; } = [];

    public FakeVectorSearchService WillFind(params ChunkMatch[] matches)
    {
        _matches.AddRange(matches);

        return this;
    }

    public Task<IReadOnlyList<ChunkMatch>> SearchAsync(
        string query,
        Guid userId,
        int? topK = null,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        Queries.Add(query);

        IReadOnlyList<ChunkMatch> found = [.. _matches.Take(topK ?? _matches.Count)];

        return Task.FromResult(found);
    }

    /// <summary>A passage with sensible defaults, so a test names only what it cares about.</summary>
    public static ChunkMatch Passage(
        string text = "Employees are entitled to twenty days of paid annual leave.",
        int page = 1,
        double similarity = 0.8,
        Guid? documentId = null,
        string fileName = "handbook.pdf") =>
        new(Guid.NewGuid(), documentId ?? Guid.NewGuid(), fileName, 0, page, page, text, similarity);
}

/// <summary>Remembers what was booked, so a test can assert that nothing was charged.</summary>
public class FakeUsageRecorder : IUsageRecorder
{
    public List<(Guid UserId, UsageKind Kind, int InputTokens, int OutputTokens, int Items)> Records { get; } = [];

    public Task RecordAsync(
        Guid userId,
        UsageKind kind,
        string model,
        int inputTokens,
        int outputTokens,
        int items,
        CancellationToken cancellationToken = default)
    {
        Records.Add((userId, kind, inputTokens, outputTokens, items));

        return Task.CompletedTask;
    }

    public Task<UsageSummary> SummariseAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var chat = Records.Where(record => record.Kind == UsageKind.Chat).ToList();
        var embedding = Records.Where(record => record.Kind == UsageKind.Embedding).ToList();

        return Task.FromResult(new UsageSummary(
            since,
            embedding.Count,
            embedding.Sum(record => record.InputTokens),
            chat.Count,
            chat.Sum(record => record.InputTokens),
            chat.Sum(record => record.OutputTokens)));
    }
}

internal static class HashExtensions
{
    /// <summary>
    /// String.GetHashCode is randomised per process, which would make a "same text, same vector"
    /// test pass or fail depending on the run. This one does not move.
    /// </summary>
    public static int GetDeterministicHash(this string text)
    {
        unchecked
        {
            var hash = (int)2166136261;

            foreach (var character in text)
            {
                hash = (hash ^ character) * 16777619;
            }

            return hash;
        }
    }
}
