using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DocuMind.Tests.Integration;

/// <summary>
/// Every failure looks the same to a client, and none of them says anything a client should not
/// hear. The browser reads <c>detail</c> and branches on <c>errorCode</c>, so both have to be there
/// whether a failure was returned by a controller or raised deeper down.
/// </summary>
[Collection("api")]
public class ErrorContractTests
{
    private readonly ApiFixture _api;

    public ErrorContractTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task A_missing_thing_is_problem_details_with_a_code_and_a_sentence()
    {
        var (client, _, _) = await _api.SignedInAsync();

        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}");
        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", problem.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task A_rejected_value_explains_itself_in_the_same_shape()
    {
        var (client, _, _) = await _api.SignedInAsync();

        var response = await client.PostAsJsonAsync(
            "/api/search/semantic", new { query = "?!" });

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", problem.ErrorCode);

        // The reason is in detail, where the client shows it — not only in the field list.
        Assert.Contains("question", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("onlyletters", "Mix letters")]
    [InlineData("shrt1!", "at least 8")]
    public async Task A_weak_password_is_refused_with_the_rule_it_broke(string password, string expected)
    {
        var client = _api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Test Person",
            email = $"user-{Guid.NewGuid():N}@example.test",
            password,
        });

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(expected, problem.Detail);
    }

    [Fact]
    public async Task A_body_that_is_not_json_is_a_bad_request_rather_than_a_crash()
    {
        var (client, _, _) = await _api.SignedInAsync();

        using var content = new StringContent("{oops", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/search/semantic", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_file_name_with_a_path_in_it_is_refused()
    {
        var (client, _, _) = await _api.SignedInAsync();

        var response = await client.PostAsJsonAsync(
            "/api/documents/upload-ticket", new { fileName = "../../etc/passwd.pdf", fileSize = 10 });

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not allowed", problem.Detail);
    }

    [Fact]
    public async Task No_failure_mentions_anything_internal()
    {
        var (client, _, _) = await _api.SignedInAsync();
        var anonymous = _api.CreateClient();

        var bodies = new List<string>
        {
            await (await client.GetAsync($"/api/documents/{Guid.NewGuid()}")).Content.ReadAsStringAsync(),
            await (await anonymous.GetAsync("/api/documents")).Content.ReadAsStringAsync(),
            await (await client.PostAsJsonAsync("/api/search/semantic", new { query = "?!" }))
                .Content.ReadAsStringAsync(),
            await (await anonymous.PostAsJsonAsync("/api/auth/login",
                new { email = "nobody@example.test", password = "Password123!" })).Content.ReadAsStringAsync(),
        };

        // Connection strings, stack traces, SQL and type names are for the log, never the response.
        foreach (var forbidden in new[]
        {
            "Npgsql", "at DocuMind", "Password=", "Host=", "SELECT ", "Exception", "secrets.json",
        })
        {
            Assert.DoesNotContain(bodies, body => body.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task A_burst_is_refused_before_the_provider_has_to_refuse_it()
    {
        // A fresh user, because the limit is counted per account — so this cannot spend another
        // test's allowance, and another test cannot spend this one's.
        var (client, _, _) = await _api.SignedInAsync();

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/search/semantic", new { query = "annual leave policy" });

            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var problem = await ReadProblemAsync(response);

                Assert.Equal("RATE_LIMITED", problem.ErrorCode);
                Assert.Contains("too often", problem.Detail);

                return;
            }
        }

        Assert.Fail($"20 requests in a row were all allowed: {string.Join(", ", statuses.Distinct())}");
    }

    private static async Task<Problem> ReadProblemAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<Problem>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problem);

        return problem! with { Detail = problem.Detail ?? string.Empty };
    }

    private sealed record Problem(string? Title, string? Detail, int? Status, string? ErrorCode, string? TraceId)
    {
        public string Detail { get; init; } = Detail ?? string.Empty;
    }
}
