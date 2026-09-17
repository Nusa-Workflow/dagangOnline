using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.RAG;

public class GuardrailService : IGuardrailService
{
    private static readonly string[] PromptInjectionPatterns = new[]
    {
        @"ignore\s+all\s+(previous|above)\s+instructions",
        @"ignore\s+previous\s+instructions",
        @"disregard\s+system\s+prompt",
        @"you\s+are\s+now\s+in\s+dan\s+mode",
        @"reveal\s+(your\s+)?(system\s+prompt|instructions)",
        @"bypass\s+(content\s+)?filter",
        @"drop\s+table",
        @"--\s*$",
        @"<script.*?>.*?</script>"
    };

    public GuardrailResultDto ValidateInput(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return new GuardrailResultDto { IsAllowed = false, ViolationReason = "Input tidak boleh kosong." };
        }

        foreach (var pattern in PromptInjectionPatterns)
        {
            if (Regex.IsMatch(userInput, pattern, RegexOptions.IgnoreCase))
            {
                return new GuardrailResultDto
                {
                    IsAllowed = false,
                    ViolationReason = "Permintaan terdeteksi mengandung instruksi yang tidak diizinkan atau manipulasi prompt.",
                    BlockedCategory = "PromptInjection"
                };
            }
        }

        return new GuardrailResultDto { IsAllowed = true };
    }

    public GuardrailResultDto ValidateRetrievalAccess(string userId, string userRole, List<RetrievalResultDto> retrievedChunks)
    {
        // Public/Guest users should only access public knowledge base documents
        // Admin or Operator can access internal operational documentation
        if (userRole == "Guest" || string.IsNullOrEmpty(userRole))
        {
            foreach (var chunk in retrievedChunks)
            {
                if (chunk.SourceUri.Contains("internal", StringComparison.OrdinalIgnoreCase) ||
                    chunk.SourceUri.Contains("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new GuardrailResultDto
                    {
                        IsAllowed = false,
                        ViolationReason = "Akses dokumen dibatasi untuk level hak akses pengguna saat ini.",
                        BlockedCategory = "AccessControl"
                    };
                }
            }
        }

        return new GuardrailResultDto { IsAllowed = true };
    }

    public GuardrailResultDto ValidateOutput(string generatedResponse, List<RetrievalResultDto> retrievedEvidence)
    {
        if (string.IsNullOrWhiteSpace(generatedResponse))
        {
            return new GuardrailResultDto { IsAllowed = false, ViolationReason = "Output kosong.", BlockedCategory = "EmptyOutput" };
        }

        // Check for potential API key or secret leakage
        if (Regex.IsMatch(generatedResponse, @"(gsk_[a-zA-Z0-9]{32,}|Bearer\s+[a-zA-Z0-9_\-\.]+)", RegexOptions.IgnoreCase))
        {
            return new GuardrailResultDto
            {
                IsAllowed = false,
                ViolationReason = "Terdeteksi kebocoran kredensial atau token rahasia pada respons.",
                BlockedCategory = "SecretLeakage"
            };
        }

        return new GuardrailResultDto { IsAllowed = true };
    }
}
