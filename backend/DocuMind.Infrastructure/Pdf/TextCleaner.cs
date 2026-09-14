using System.Text;
using System.Text.RegularExpressions;
using DocuMind.Application.Interfaces;

namespace DocuMind.Infrastructure.Pdf;

/// <summary>
/// Normalises extracted text.
///
/// Every rule here is deliberately conservative. Retrieval quality depends on the words that
/// survive, so this removes layout artefacts — control characters, hyphens split across lines,
/// runs of whitespace — and leaves the prose alone. Anything cleverer (stripping headers and
/// footers, de-duplicating repeated page furniture) risks deleting real content and belongs
/// behind measurements, not guesses.
/// </summary>
public partial class TextCleaner : ITextCleaner
{
    public string Clean(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var text = rawText;

        // Normalise line endings first so every later rule only has to consider "\n".
        text = text.ReplaceLineEndings("\n");

        text = RemoveControlCharacters(text);

        // "informa-\ntion" is one word broken by the PDF's line wrap, not two.
        text = SoftHyphenBreaks().Replace(text, "$1$2");

        // Collapse runs of spaces and tabs, but not newlines — paragraph structure is what the
        // chunker later uses to find sensible split points.
        text = HorizontalWhitespace().Replace(text, " ");

        // Three or more blank lines carry no more meaning than one blank line.
        text = ExcessiveBlankLines().Replace(text, "\n\n");

        // Trailing spaces before a newline are pure noise.
        text = TrailingSpaces().Replace(text, "\n");

        return text.Trim();
    }

    public IReadOnlyList<ExtractedPage> CleanPages(IReadOnlyList<ExtractedPage> pages)
    {
        var cleaned = new List<ExtractedPage>(pages.Count);

        foreach (var page in pages)
        {
            var text = Clean(page.Text);

            // A blank page contributes nothing to retrieval, but the pages that follow keep their
            // original numbers — renumbering here would make every later citation point one page
            // off for any document containing a blank.
            if (text.Length > 0)
            {
                cleaned.Add(new ExtractedPage(page.Number, text));
            }
        }

        return cleaned;
    }

    /// <summary>
    /// Strips characters that carry no textual meaning. Tab, newline and carriage return are
    /// preserved; the replacement character U+FFFD is dropped because it marks a glyph the
    /// extractor could not map, and leaving it in pollutes embeddings.
    /// </summary>
    private static string RemoveControlCharacters(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            if (character is '\n' or '\t')
            {
                builder.Append(character);
                continue;
            }

            if (char.IsControl(character) || character == '�')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"(\w)-\n\s*(\w)")]
    private static partial Regex SoftHyphenBreaks();

    [GeneratedRegex(@"[^\S\n]+")]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveBlankLines();

    [GeneratedRegex(@"[ \t]+\n")]
    private static partial Regex TrailingSpaces();
}
