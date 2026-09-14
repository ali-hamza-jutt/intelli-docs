namespace DocuMind.Domain.Entities;

/// <summary>Shared counting rules, so a page and its parent document never disagree.</summary>
internal static class TextStatistics
{
    private static readonly char[] WordSeparators = [' ', '\t', '\n', '\r', '\f', '\v'];

    public static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
