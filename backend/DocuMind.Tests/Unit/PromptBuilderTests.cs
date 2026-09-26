using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Ai;
using DocuMind.Tests.Doubles;

namespace DocuMind.Tests.Unit;

/// <summary>
/// The prompt is the only thing standing between a retrieved passage and an invented answer, so its
/// rules are worth asserting rather than trusting. These tests do not judge the wording — they check
/// that the instructions a grounded answer depends on are actually being sent.
/// </summary>
public class PromptBuilderTests
{
    private readonly GroundedPromptBuilder _builder = new();

    private static ChunkMatch Passage(string text, int page, string file = "handbook.pdf") =>
        new(Guid.NewGuid(), Guid.NewGuid(), file, 0, page, page, text, 0.8);

    [Fact]
    public void Tells_the_model_to_answer_only_from_the_passages()
    {
        var prompt = _builder.BuildAnswerPrompt("How much leave?", [Passage("Twenty days.", 1)]);

        Assert.Contains("Answer only from the numbered passages", prompt.System);
        Assert.Contains("Do not use outside knowledge", prompt.System);
    }

    [Fact]
    public void Tells_the_model_to_refuse_rather_than_improvise()
    {
        var prompt = _builder.BuildAnswerPrompt("How much leave?", [Passage("Twenty days.", 1)]);

        Assert.Contains("do not contain the answer", prompt.System);
        Assert.Contains("Never invent", prompt.System);
    }

    [Fact]
    public void Numbers_the_passages_from_one_and_labels_where_each_came_from()
    {
        var prompt = _builder.BuildAnswerPrompt(
            "How much leave?",
            [
                Passage("Twenty days of annual leave.", 1),
                Passage("Ten days of sick leave.", 4, "policy.pdf"),
            ]);

        Assert.Contains("[1] handbook.pdf, page 1:", prompt.User);
        Assert.Contains("[2] policy.pdf, page 4:", prompt.User);
        Assert.Contains("Twenty days of annual leave.", prompt.User);
        Assert.Contains("Ten days of sick leave.", prompt.User);
    }

    [Fact]
    public void Says_pages_when_a_passage_straddles_a_break()
    {
        var straddling = new ChunkMatch(
            Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", 0, 7, 8, "Across the break.", 0.8);

        var prompt = _builder.BuildAnswerPrompt("Anything?", [straddling]);

        Assert.Contains("pages 7–8", prompt.User);
    }

    [Fact]
    public void Puts_the_question_last_where_the_model_reads_it()
    {
        var prompt = _builder.BuildAnswerPrompt("How much leave?", [Passage("Twenty days.", 1)]);

        Assert.EndsWith("How much leave?", prompt.User);
    }

    [Fact]
    public void Shows_earlier_turns_above_the_passages_and_marks_them_as_context()
    {
        var prompt = _builder.BuildAnswerPrompt(
            "And carrying it over?",
            [Passage("Carry-over lasts one quarter.", 2)],
            [
                new RagTurn(FromUser: true, "How much annual leave?"),
                new RagTurn(FromUser: false, "Twenty days."),
            ]);

        Assert.Contains("User: How much annual leave?", prompt.User);
        Assert.Contains("Assistant: Twenty days.", prompt.User);

        // History sits above the passages, and the rules say it is not evidence — otherwise a model
        // can answer from something it said earlier rather than from the document.
        Assert.True(
            prompt.User.IndexOf("Earlier in this conversation", StringComparison.Ordinal)
            < prompt.User.IndexOf("Passages:", StringComparison.Ordinal));

        Assert.Contains("They are not evidence", prompt.System);
    }

    [Fact]
    public void Says_nothing_about_earlier_turns_when_there_are_none()
    {
        var prompt = _builder.BuildAnswerPrompt("How much leave?", [Passage("Twenty days.", 1)]);

        Assert.DoesNotContain("Earlier in this conversation", prompt.User);
    }

    [Fact]
    public void Gives_the_model_only_what_the_citation_is_built_from()
    {
        // The page and file name go in as context, but the citation the user sees is built in code
        // from the same passage — so the model is never the source of a page number.
        var passage = Passage("Twenty days.", 3);
        var prompt = _builder.BuildAnswerPrompt("How much leave?", [passage]);

        Assert.Contains("page 3", prompt.User);
        Assert.DoesNotContain(passage.ChunkId.ToString(), prompt.User);
        Assert.DoesNotContain(passage.DocumentId.ToString(), prompt.User);
    }
}
