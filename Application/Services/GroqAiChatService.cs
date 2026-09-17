using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace dagangOnline.Application.Services;

public class GroqAiChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqAiChatService> _logger;
    private readonly string _apiKey;
    private readonly string _modelName;

    public GroqAiChatService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqAiChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? configuration["Groq:ApiKey"] ?? "";
        _modelName = Environment.GetEnvironmentVariable("GROQ_MODEL") ?? configuration["Groq:Model"] ?? "qwen/qwen3.8-27b";
        
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "dagangOnline/1.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<AiSuggestionDto> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = $@"Anda adalah Asisten AI Copilot Customer Service untuk platform dagangOnline (sahabat UMKM dan perdagangan lokal).
PANDUAN BAHASA & KOMUNIKASI:
- Jawablah SELALU dalam bahasa yang SAMA dengan bahasa yang digunakan pengguna:
  * Jika pengguna menggunakan Basa Sunda (contoh: 'kumaha ieu', 'nuhun', 'mésér'), balaslah dalam Basa Sunda yang santun, akrab, dan alami.
  * Jika pengguna menggunakan Basa Jawa (contoh: 'neng endi', 'piye carane', 'matur nuwun'), balaslah dalam Basa Jawa yang ramah, luwes, dan solutif.
  * Jika pengguna menggunakan Bahasa Indonesia, balaslah dalam Bahasa Indonesia yang profesional dan ramah.
  * Jika pengguna menggunakan English, balaslah dalam English yang ringkas dan jelas.
- Bersikap sopan, solutif, dan bantu pengguna memahami layanan belanja, produk, ongkir, atau kemitraan dagangOnline.

Format response dalam format JSON yang valid:
{{
  ""suggested_response"": ""jawaban lengkap Anda untuk pengguna dalam bahasa yang sesuai"",
  ""intent"": ""product_information, order_status, complaint, technical_support, human_request, general"",
  ""priority"": ""Normal"",
  ""needs_human"": false,
  ""reason"": """"
}}

Gunakan informasi katalog/konteks berikut untuk memperkaya jawaban Anda:
[KONTEKS]
{contextData}
[AKHIR KONTEKS]";

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                response_format = new { type = "json_object" },
                temperature = 0.5,
                max_tokens = 512
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", jsonContent, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Groq API returned HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errBody);
                throw new HttpRequestException($"Groq API error {response.StatusCode}: {errBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);
            
            var answerStr = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(answerStr)) 
            {
                throw new Exception("Empty response from AI");
            }

            // Clean markdown fences if model returned ```json ... ```
            var cleanJson = answerStr.Trim();
            if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Substring(7);
            }
            if (cleanJson.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Substring(3);
            }
            if (cleanJson.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
            }
            cleanJson = cleanJson.Trim();

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dto = JsonSerializer.Deserialize<AiSuggestionDto>(cleanJson, options);
                if (dto != null && !string.IsNullOrWhiteSpace(dto.SuggestedResponse))
                {
                    return dto;
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed parsing JSON response from model, attempting raw fallback");
            }

            // Fallback: If not JSON object, return the raw text as suggested response
            return new AiSuggestionDto
            {
                SuggestedResponse = cleanJson,
                Intent = "general",
                Priority = "Normal",
                NeedsHuman = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            return new AiSuggestionDto 
            { 
                SuggestedResponse = "Maaf, koneksi ke asisten AI sedang terganggu. Anda dapat memilih tab 'Hubungi Human Agent' untuk terhubung dengan Customer Service.",
                NeedsHuman = true,
                Priority = "High",
                Reason = ex.Message
            };
        }
    }
}
