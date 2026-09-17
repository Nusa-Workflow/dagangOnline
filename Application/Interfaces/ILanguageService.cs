using System.Threading;
using System.Threading.Tasks;

namespace dagangOnline.Application.Interfaces;

public interface ILanguageService
{
    string DetectLanguage(string text);
    string NormalizeQuery(string text, string languageCode);
    string? TranslateTerm(string term, string fromLanguage, string toLanguage);
}
