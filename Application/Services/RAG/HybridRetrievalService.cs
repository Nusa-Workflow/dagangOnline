using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using dagangOnline.Data;

namespace dagangOnline.Application.Services.RAG;

public class HybridRetrievalService : IRetrievalService
{
    private readonly ApplicationDbContext _db;
    private readonly IRerankerService _rerankerService;

    public HybridRetrievalService(ApplicationDbContext db, IRerankerService rerankerService)
    {
        _db = db;
        _rerankerService = rerankerService;
    }

    private static double CalculateCosineSimilarity(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length == 0 || v2.Length == 0 || v1.Length != v2.Length)
            return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            normA += v1[i] * v1[i];
            normB += v2[i] * v2[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public async Task<List<RetrievalResultDto>> RetrieveDenseAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        // Fetch candidate chunks that have embeddings
        var chunks = await _db.KnowledgeChunks
            .Include(c => c.Document)
            .Where(c => c.EmbeddingJson != null)
            .Take(100) // retrieve candidates for scoring
            .ToListAsync(cancellationToken);

        var ranked = chunks
            .Select(c => new RetrievalResultDto
            {
                ChunkId = c.Id,
                DocumentId = c.KnowledgeDocumentId,
                Content = c.Content,
                DocumentTitle = c.Document?.Title ?? "Dokumen",
                SourceUri = c.Document?.Category ?? "",
                Score = CalculateCosineSimilarity(queryEmbedding, c.Embedding!),
                RetrievalType = "Dense",
                Language = c.Document?.Language ?? "id-ID"
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        return ranked;
    }

    public async Task<List<RetrievalResultDto>> RetrieveSparseAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                         .Where(t => t.Length > 2)
                         .Take(5)
                         .ToList();

        if (!terms.Any())
        {
            return new List<RetrievalResultDto>();
        }

        var chunks = await _db.KnowledgeChunks
            .Include(c => c.Document)
            .Take(100)
            .ToListAsync(cancellationToken);

        var candidates = chunks
            .Where(c => terms.Any(t => c.Content.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var results = candidates.Select(c =>
        {
            int matchCount = terms.Count(t => c.Content.Contains(t, StringComparison.OrdinalIgnoreCase));
            double score = (double)matchCount / terms.Count;
            return new RetrievalResultDto
            {
                ChunkId = c.Id,
                DocumentId = c.KnowledgeDocumentId,
                Content = c.Content,
                DocumentTitle = c.Document?.Title ?? "Dokumen",
                SourceUri = c.Document?.Category ?? "",
                Score = score,
                RetrievalType = "Sparse",
                Language = c.Document?.Language ?? "id-ID"
            };
        })
        .OrderByDescending(r => r.Score)
        .Take(topK)
        .ToList();

        return results;
    }

    public async Task<List<RetrievalResultDto>> RetrieveHybridAsync(string query, float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var dense = await RetrieveDenseAsync(queryEmbedding, topK, cancellationToken);
        var sparse = await RetrieveSparseAsync(query, topK, cancellationToken);

        return _rerankerService.Rerank(dense, sparse, topK);
    }
}
