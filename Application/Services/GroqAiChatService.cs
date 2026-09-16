using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
        
        _apiKey = configuration["Groq:ApiKey"] ?? "placeholder_key";
        _modelName = configuration["Groq:Model"] ?? "qwen-2.5-32b"; // fallback to a known Qwen model on Groq if not configured
        
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = $@"Anda adalah Asisten AI untuk platform dagangOnline.
Tugas Anda adalah membantu pengguna (pembeli atau mitra) dengan menjawab pertanyaan mereka secara ramah dan profesional.
Anda harus menggunakan informasi katalog berikut untuk menjawab pertanyaan mengenai produk:

[DATA KATALOG]
{contextData}
[AKHIR DATA KATALOG]

Jika pengguna menanyakan sesuatu yang tidak ada di katalog, jawab dengan sopan bahwa Anda belum memiliki informasi tersebut.";

            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                },
                temperature = 0.5,
                max_tokens = 512
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("chat/completions", jsonContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseJson);
            
            var answer = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return answer ?? "Maaf, saya tidak dapat memproses permintaan Anda saat ini.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            return "Maaf, terjadi kesalahan saat menghubungi layanan AI.";
        }
    }
}
