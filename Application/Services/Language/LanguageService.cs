using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.Language;

public class LanguageService : ILanguageService
{
    // Specific dialect/language marker tokens
    private static readonly HashSet<string> JavaneseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "piye", "kepiye", "tuku", "iki", "kui", "karo", "nggo", "ora", "iso", "dolan",
        "carane", "matur", "nuwun", "nyuwun", "sinten", "pundi", "mboten", "inggil", "ngendi", "kados"
    };

    private static readonly HashSet<string> SundaneseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "kumaha", "mésér", "meser", "ieu", "itu", "jeung", "pikeun", "teu", "tiasa", "saha",
        "naha", "iraha", "timana", "hatur", "nuhun", "punten", "abdi", "anjeun", "enya", "sanes"
    };

    private static readonly HashSet<string> EnglishTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "how", "what", "where", "when", "why", "who", "can", "could", "would", "buy",
        "purchase", "order", "price", "is", "are", "do", "does", "product", "service", "help", "please", "thank"
    };

    private static readonly Dictionary<string, string> GlossaryToIndonesian = new(StringComparer.OrdinalIgnoreCase)
    {
        // Javanese to Indonesian
        { "tuku", "beli" },
        { "carane", "bagaimana caranya" },
        { "piye", "bagaimana" },
        { "kepiye", "bagaimana" },
        { "iki", "ini" },
        { "ora", "tidak" },
        { "iso", "bisa" },
        { "nuwun", "terima kasih" },
        { "matur nuwun", "terima kasih" },
        
        // Sundanese to Indonesian
        { "meser", "beli" },
        { "mésér", "beli" },
        { "kumaha", "bagaimana" },
        { "cara mésér", "cara membeli" },
        { "cara meser", "cara membeli" },
        { "ieu", "ini" },
        { "teu", "tidak" },
        { "tiasa", "bisa" },
        { "hatur nuhun", "terima kasih" },
        { "punten", "permisi" }
    };

    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "id-ID";

        var words = Regex.Matches(text.ToLowerInvariant(), @"\b[\w'-]+\b")
                         .Select(m => m.Value)
                         .ToList();

        if (words.Count == 0) return "id-ID";

        int jvScore = words.Count(w => JavaneseTokens.Contains(w));
        int suScore = words.Count(w => SundaneseTokens.Contains(w));
        int enScore = words.Count(w => EnglishTokens.Contains(w));

        // Javanese match
        if (jvScore > 0 && jvScore >= suScore && jvScore >= enScore)
        {
            return "jv-ID";
        }

        // Sundanese match
        if (suScore > 0 && suScore > jvScore && suScore >= enScore)
        {
            return "su-ID";
        }

        // English match
        if (enScore > 0 && enScore > jvScore && enScore > suScore)
        {
            return "en-US";
        }

        return "id-ID"; // Default Indonesian
    }

    public string NormalizeQuery(string text, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Clean extra spaces & punctuation
        var cleaned = Regex.Replace(text.Trim(), @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"[^\w\s-]", "");

        return cleaned;
    }

    public string? TranslateTerm(string term, string fromLanguage, string toLanguage)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;

        if (fromLanguage.Equals("jv-ID", StringComparison.OrdinalIgnoreCase) ||
            fromLanguage.Equals("su-ID", StringComparison.OrdinalIgnoreCase))
        {
            if (GlossaryToIndonesian.TryGetValue(term.Trim(), out var idTerm))
            {
                return idTerm;
            }
        }

        return null;
    }
}
