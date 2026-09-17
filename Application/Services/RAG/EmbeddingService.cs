using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.RAG;

public class EmbeddingService : IEmbeddingService
{
    private const int EmbeddingDimension = 1536;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new float[EmbeddingDimension]);
        }

        // Fast deterministic, normalized pseudo-embedding based on SHA-256 token hashing
        // This ensures fully reproducible dense vector operations in offline/local dev environments
        // while remaining 100% compatible with pgvector cosine distance operations (<=>).
        var vector = new float[EmbeddingDimension];
        var tokens = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(token));

            for (int i = 0; i < 16; i++)
            {
                int index = (hash[i * 2] << 8 | hash[i * 2 + 1]) % EmbeddingDimension;
                vector[index] += 1.0f;
            }
        }

        // Normalize vector to unit length (L2 norm)
        float sumSquares = 0f;
        for (int i = 0; i < EmbeddingDimension; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        if (sumSquares > 0)
        {
            float norm = MathF.Sqrt(sumSquares);
            for (int i = 0; i < EmbeddingDimension; i++)
            {
                vector[i] /= norm;
            }
        }

        return Task.FromResult(vector);
    }
}
