using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Interfaces;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Tests.Doubles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace DocuMind.Tests.Integration;

/// <summary>
/// The real application, started in-process against a database made for the run.
///
/// Real all the way down except the two services that cost money: routing, authentication,
/// validation, rate limiting, the error shape, the background worker and every SQL query are the
/// ones that ship. The AI provider is a fake, because a test should not depend on a network, a
/// credential, or a model's mood — and because what these tests check is this code's behaviour
/// around a provider, not the provider.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabase = "documind_api_tests";

    private string _connectionString = string.Empty;

    /// <summary>The fakes, exposed so a test can say what the model returns and what it was asked.</summary>
    public FakeLlmService Llm { get; } = new();

    public FakeEmbeddingService Embeddings { get; } = new();

    /// <summary>Where uploaded files land, cleaned up with the fixture.</summary>
    public string UploadDirectory { get; } =
        Path.Combine(Path.GetTempPath(), $"documind-tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting rather than ConfigureAppConfiguration: the application reads these while its
        // services are being registered, which happens before an added configuration source would
        // be visible. These are in place from the first line of Program.
        var settings = new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = _connectionString,

            // Local disk, so no test ever reaches a storage provider over the network.
            ["Storage:Provider"] = "Local",
            ["Storage:Local:UploadDirectory"] = UploadDirectory,

            // A key has to be present for the app to consider the provider configured. What it is
            // never matters: the services it would be sent to are replaced below.
            // The similarity floor that ships is tuned to a real embedding model. The fake above
            // scores on shared words alone, so its numbers are smaller — but unrelated text shares
            // no words at all and still scores zero, which is the separation these tests rely on.
            // Where the floor itself belongs is settled in the unit tests.
            ["Rag:SimilarityThreshold"] = "0.05",

            ["AI:ApiKey"] = "not-a-real-key",
            ["AI:Endpoint"] = "http://localhost:1/unused",

            ["Jwt:Key"] = "a-test-signing-key-long-enough-to-be-accepted-by-the-validator",
            ["Jwt:Issuer"] = "DocuMind.Api",
            ["Jwt:Audience"] = "DocuMind.Spa",
        };

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILLMService>();
            services.RemoveAll<IEmbeddingService>();

            services.AddSingleton<ILLMService>(Llm);
            services.AddSingleton<IEmbeddingService>(Embeddings);
        });
    }

    /// <summary>A client signed in as a brand-new user, and that user's id.</summary>
    public async Task<(HttpClient Client, Guid UserId, string Email)> SignedInAsync(string? name = null)
    {
        var client = CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.test";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = name ?? "Test Person",
            email,
            password = "Password123!",
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RegisterResult>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return (client, body.User.Id, email);
    }

    public async Task InitializeAsync()
    {
        var server = ServerConnectionString();

        await using (var connection = new NpgsqlConnection(server))
        {
            await connection.OpenAsync();

            foreach (var sql in new[]
            {
                $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)",
                $"CREATE DATABASE \"{TestDatabase}\"",
            })
            {
                await using var command = new NpgsqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        _connectionString = new NpgsqlConnectionStringBuilder(server) { Database = TestDatabase }
            .ConnectionString;

        Directory.CreateDirectory(UploadDirectory);

        // Starting the host applies the schema, so the tests run against the same migrations the
        // application does rather than a model snapshot.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DocuMindDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(ServerConnectionString());
        await connection.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)", connection);

        await drop.ExecuteNonQueryAsync();

        if (Directory.Exists(UploadDirectory))
        {
            Directory.Delete(UploadDirectory, recursive: true);
        }
    }

    /// <summary>
    /// The development server, from user secrets, pointed at the maintenance database so the test
    /// one can be created. Never a connection string in the repository.
    /// </summary>
    private static string ServerConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("527e50e2-9634-46bd-912a-cef741e5590c")
            .AddEnvironmentVariables()
            .Build();

        var development = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No ConnectionStrings:DefaultConnection in user secrets. These tests need a Postgres server.");

        return new NpgsqlConnectionStringBuilder(development) { Database = "postgres" }.ConnectionString;
    }

    private sealed record RegisterResult(string AccessToken, UserSummary User);

    private sealed record UserSummary(Guid Id, string Name, string Email);
}

/// <summary>
/// One host for every integration test. They share a database, so they run one at a time — which
/// also keeps the rate-limit test from spending another test's allowance.
/// </summary>
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
