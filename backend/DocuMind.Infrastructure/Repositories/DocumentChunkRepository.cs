using System.Data;
using System.Globalization;
using DocuMind.Application.Interfaces;
using DocuMind.Domain.Entities;
using DocuMind.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace DocuMind.Infrastructure.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly DocuMindDbContext _context;

    public DocumentChunkRepository(DocuMindDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks)
    {
        await _context.DocumentChunks.AddRangeAsync(chunks);
    }

    public async Task<List<DocumentChunk>> GetForDocumentAsync(Guid documentId, int offset, int limit)
    {
        return await _context.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> CountForDocumentAsync(Guid documentId)
    {
        return await _context.DocumentChunks
            .CountAsync(c => c.DocumentId == documentId);
    }

    public async Task RemoveForDocumentAsync(Guid documentId)
    {
        await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Written as SQL because LINQ has no way to express <c>&lt;=&gt;</c>, and because the ordering
    /// has to be the one the HNSW index was built for. The ranking happens inside the subquery —
    /// ordering and limiting there is what lets the index do the work — and the similarity floor is
    /// applied outside it, where it cannot interfere with the index.
    /// </summary>
    public async Task<List<ChunkMatch>> SearchAsync(
        float[] embedding,
        Guid userId,
        int topK,
        Guid? documentId,
        double minimumSimilarity,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT ranked."Id", ranked."DocumentId", ranked."FileName", ranked."ChunkIndex",
                   ranked."PageNumber", ranked."EndPageNumber", ranked."Text", ranked.similarity
            FROM (
                SELECT c."Id", c."DocumentId", d."FileName", c."ChunkIndex", c."PageNumber",
                       c."EndPageNumber", c."Text",
                       1 - (c."Embedding" <=> @query) AS similarity
                FROM "DocumentChunks" AS c
                INNER JOIN "Documents" AS d ON d."Id" = c."DocumentId"
                WHERE d."UserId" = @userId
                  AND c."Embedding" IS NOT NULL
                  AND (@documentId IS NULL OR c."DocumentId" = @documentId)
                ORDER BY c."Embedding" <=> @query
                LIMIT @topK
            ) AS ranked
            WHERE ranked.similarity >= @threshold
            ORDER BY ranked.similarity DESC
            """;

        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = new NpgsqlCommand(sql, connection);

        // Sent as the vector's own text form and cast by Postgres, so the query needs no vector
        // type mapping on the ADO connection.
        command.Parameters.Add(new NpgsqlParameter("query", NpgsqlDbType.Unknown)
        {
            Value = Format(embedding)
        });
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.Add(new NpgsqlParameter("documentId", NpgsqlDbType.Uuid)
        {
            Value = documentId.HasValue ? documentId.Value : DBNull.Value
        });
        command.Parameters.AddWithValue("topK", topK);
        command.Parameters.AddWithValue("threshold", minimumSimilarity);

        var matches = new List<ChunkMatch>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            matches.Add(new ChunkMatch(
                ChunkId: reader.GetGuid(0),
                DocumentId: reader.GetGuid(1),
                FileName: reader.GetString(2),
                ChunkIndex: reader.GetInt32(3),
                PageNumber: reader.GetInt32(4),
                EndPageNumber: reader.GetInt32(5),
                Text: reader.GetString(6),
                Similarity: reader.GetDouble(7)));
        }

        return matches;
    }

    /// <summary>The literal pgvector accepts: <c>[0.1,0.2,…]</c>, invariant so a comma decimal
    /// separator on the machine's locale cannot corrupt it.</summary>
    private static string Format(float[] embedding)
    {
        return $"[{string.Join(',', embedding.Select(v => v.ToString(CultureInfo.InvariantCulture)))}]";
    }
}
