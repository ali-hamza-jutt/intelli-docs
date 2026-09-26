namespace DocuMind.Tests.Integration;

/// <summary>
/// A real six-page PDF with real text in it, so extraction and chunking are exercised rather than
/// stubbed. It is a made-up staff handbook — leave, sick days, parental leave, remote work — which
/// is also what the questions in these tests ask about.
/// </summary>
public static class PdfFixture
{
    public static byte[] Bytes { get; } =
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "handbook.pdf"));

    /// <summary>A phrase the document really contains, for asserting that text survived the trip.</summary>
    public const string KnownPhrase = "annual leave";
}
