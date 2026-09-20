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
        "carane", "matur", "nuwun", "nyuwun", "sinten", "pundi", "mboten", "inggil", "ngendi", "kados", "badhe", "sedaya"
    };

    private static readonly HashSet<string> SundaneseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "kumaha", "mésér", "meser", "ieu", "itu", "jeung", "pikeun", "teu", "tiasa", "saha",
        "naha", "iraha", "timana", "hatur", "nuhun", "punten", "abdi", "anjeun", "enya", "sanes", "sadaya", "dugi"
    };

    private static readonly HashSet<string> MinangTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "baa", "ba'a", "mambali", "bilo", "sia", "dima", "ambo", "awak", "tarimo", "kasih",
        "ondeh", "lamak", "rancak", "lah", "alah", "kaba", "galeh", "pitih", "urang", "dunsanak"
    };

    private static readonly HashSet<string> MaduraTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "dekremma", "kemma", "ba'na", "bâ'na", "sengko", "sengko'", "melle", "tadek", "tadhe'",
        "bada", "sakone", "matoer", "sakalangkong", "kabbhi", "oreng", "taretan", "apah"
    };

    private static readonly HashSet<string> BaliTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "kenken", "ngadep", "beli", "meli", "dija", "tiang", "ragane", "sing", "ada",
        "suksma", "matur", "napi", "swastyastu", "om", "semeton", "mangkin", "becik"
    };

    private static readonly HashSet<string> BugisTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "pekkogi", "tega", "melli", "iyarega", "degaga", "engka", "kurru", "sumange",
        "aga", "kareba", "tabe", "tabe'", "siddi", "silessureng", "maraja", "idi'"
    };

    private static readonly HashSet<string> BanjarTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "kayapa", "manukar", "tukar", "dimapa", "ulun", "pian", "baisi", "kada", "bisa",
        "tarima", "handak", "hanyar", "mun", "nang", "bubuhan", "ganal", "lah"
    };

    private static readonly HashSet<string> BatakTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "boha", "songondia", "tuani", "tuaha", "ahu", "hamu", "manuhor", "mauliate",
        "horas", "didia", "piga", "dongan", "hita", "nanget", "mardagang"
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
        { "punten", "permisi" },

        // Minang to Indonesian
        { "mambali", "beli" },
        { "baa caronyo", "bagaimana caranya" },
        { "baa", "bagaimana" },
        { "tarimo kasih", "terima kasih" },
        { "pitih", "uang / modal" },

        // Madura to Indonesian
        { "melle", "beli" },
        { "dekremma", "bagaimana" },
        { "sakalangkong", "terima kasih" },

        // Bali to Indonesian
        { "meli", "beli" },
        { "kenken", "bagaimana" },
        { "suksma", "terima kasih" },

        // Bugis to Indonesian
        { "melli", "beli" },
        { "pekkogi", "bagaimana" },
        { "kurru sumange", "terima kasih" },

        // Banjar to Indonesian
        { "manukar", "beli" },
        { "kayapa", "bagaimana" },
        { "tarima kasih", "terima kasih" },

        // Batak to Indonesian
        { "manuhor", "beli" },
        { "boha", "bagaimana" },
        { "mauliate", "terima kasih" }
    };

    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "id-ID";

        var words = Regex.Matches(text.ToLowerInvariant(), @"\b[\w'-]+\b")
                         .Select(m => m.Value)
                         .ToList();

        if (words.Count == 0) return "id-ID";

        var scores = new Dictionary<string, int>
        {
            ["jv-ID"] = words.Count(w => JavaneseTokens.Contains(w)),
            ["su-ID"] = words.Count(w => SundaneseTokens.Contains(w)),
            ["min-ID"] = words.Count(w => MinangTokens.Contains(w)),
            ["mad-ID"] = words.Count(w => MaduraTokens.Contains(w)),
            ["ban-ID"] = words.Count(w => BaliTokens.Contains(w)),
            ["bug-ID"] = words.Count(w => BugisTokens.Contains(w)),
            ["bjn-ID"] = words.Count(w => BanjarTokens.Contains(w)),
            ["btk-ID"] = words.Count(w => BatakTokens.Contains(w)),
            ["en-US"] = words.Count(w => EnglishTokens.Contains(w))
        };

        var highest = scores.OrderByDescending(kv => kv.Value).First();
        if (highest.Value > 0)
        {
            return highest.Key;
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

        if (GlossaryToIndonesian.TryGetValue(term.Trim(), out var idTerm))
        {
            return idTerm;
        }

        return null;
    }
}
