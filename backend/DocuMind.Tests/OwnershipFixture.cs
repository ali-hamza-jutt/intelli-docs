using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DocuMind.Tests;

/// <summary>
/// A real Postgres database, created for the run and dropped at the end of it.
///
/// Real rather than in-memory because the thing under test is the SQL: vector search is raw SQL
/// against pgvector, and ownership is a predicate inside it. An in-memory provider would run
/// different queries and prove nothing about the ones that ship.
///
/// The server comes from the same user secret the API uses, and the tests get their own database on
/// it, so a failing test can never damage development data.
/// </summary>
public class OwnershipFixture : IAsyncLifetime
{
    private const string TestDatabase = "documind_ownership_tests";

    public Guid AliceId { get; private set; }
    public Guid BobId { get; private set; }

    /// <summary>Alice's document, chunks and conversation. Bob owns an identical copy of each.</summary>
    public Guid AliceDocumentId { get; private set; }
    public Guid BobDocumentId { get; private set; }
    public Guid AliceConversationId { get; private set; }
    public Guid BobConversationId { get; private set; }

    /// <summary>The vector both users' chunks were given, so similarity cannot be what separates them.</summary>
    public float[] SharedEmbedding { get; } =
        [.. Enumerable.Range(0, DocumentChunk.EmbeddingDimensions).Select(i => (float)(i % 7) / 7f)];

    private string _connectionString = string.Empty;

    public DocuMindDbContext NewContext()
    {
        return new DocuMindDbContext(
            new DbContextOptionsBuilder<DocuMindDbContext>()
                .UseNpgsql(_connectionString, npgsql => npgsql.UseVector())
                .Options);
    }

    public async Task InitializeAsync()
    {
        var server = ServerConnectionString();

        await RecreateDatabaseAsync(server);

        _connectionString = new NpgsqlConnectionStringBuilder(server) { Database = TestDatabase }
            .ConnectionString;

        await using var context = NewContext();
        await context.Database.MigrateAsync();

        await SeedAsync(context);
    }

    public async Task DisposeAsync()
    {
        // Npgsql keeps connections pooled; the database cannot be dropped while they are open.
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(ServerConnectionString());
        await connection.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)", connection);

        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// The development connection string, pointed at the maintenance database so the test database
    /// can be created. Read from user secrets — never from a file in the repository.
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

    private static async Task RecreateDatabaseAsync(string server)
    {
        await using var connection = new NpgsqlConnection(server);
        await connection.OpenAsync();

        foreach (var sql in new[]
        {
            $"DROP DATABASE IF EXISTS \"{TestDatabase}\" WITH (FORCE)",
            $"CREATE DATABASE \"{TestDatabase}\""
        })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Two users with the same documents, the same text and the same vectors. Everything that could
    /// distinguish them is identical, so only the ownership predicate can tell them apart.
    /// </summary>
    private async Task SeedAsync(DocuMindDbContext context)
    {
        var alice = User.Create("Alice", "alice@example.test", "hash");
        var bob = User.Create("Bob", "bob@example.test", "hash");

        context.Users.AddRange(alice, bob);

        AliceId = alice.Id;
        BobId = bob.Id;

        AliceDocumentId = await SeedDocumentAsync(context, alice.Id);
        BobDocumentId = await SeedDocumentAsync(context, bob.Id);

        AliceConversationId = SeedConversation(context, alice.Id, AliceDocumentId);
        BobConversationId = SeedConversation(context, bob.Id, BobDocumentId);

        await context.SaveChangesAsync();
    }

    private async Task<Guid> SeedDocumentAsync(DocuMindDbContext context, Guid userId)
    {
        var document = Document.Upload(
            userId: userId,
            originalFileName: "handbook.pdf",
            storedFileName: $"{Guid.NewGuid()}.pdf",
            filePath: $"uploads/{Guid.NewGuid()}.pdf",
            contentType: "application/pdf",
            fileSize: 1024);

        document.MarkCompleted();

        context.Documents.Add(document);

        var chunk = DocumentChunk.Create(
            document.Id, 0, "Employees are entitled to twenty days of paid annual leave.", 1, 1);

        chunk.AttachEmbedding(SharedEmbedding);
        context.DocumentChunks.Add(chunk);

        await Task.CompletedTask;

        return document.Id;
    }

    private static Guid SeedConversation(DocuMindDbContext context, Guid userId, Guid documentId)
    {
        var conversation = Conversation.Start(userId, documentId, "handbook.pdf");
        var question = conversation.AskedBy("How much annual leave do we get?", isFirstTurn: true);
        var answer = conversation.AnsweredWith("Twenty days. [1]", true, "test-model", 10, 5);

        answer.Cite(1, documentId, "handbook.pdf", 1, 1, "Employees are entitled to twenty days.", 0.9);

        context.Conversations.Add(conversation);
        context.ChatMessages.AddRange(question, answer);

        return conversation.Id;
    }
}
