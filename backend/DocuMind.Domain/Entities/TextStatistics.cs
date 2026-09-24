namespace DocuMind.Domain.Entities;

/// <summary>Shared counting rules, so a page, a chunk and their document never disagree.</summary>
internal static class TextStatistics
{
    private static readonly char[] WordSeparators = [' ', '\t', '\n', '\r', '\f', '\v'];

    /// <summary>
    /// Roughly four characters of English per token. An approximation — the exact count depends on
    /// the model's tokenizer — but close enough to budget prompts against, and it needs no library.
    /// </summary>
    private const double CharactersPerToken = 4.0;

    public static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static int EstimateTokens(string text)
    {
        return string.IsNullOrEmpty(text)
            ? 0
            : (int)Math.Ceiling(text.Length / CharactersPerToken);
    }
}
