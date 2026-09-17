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
        
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? configuration["Groq:ApiKey"] ?? "placeholder_key";
        _modelName = configuration["Groq:Model"] ?? "qwen/qwen3.8-27b";
        
        _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<AiSuggestionDto> GetChatResponseAsync(string userMessage, string contextData, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemPrompt = $@"Anda adalah Asisten AI Copilot untuk platform dagangOnline.
Tugas Anda adalah membaca pesan pengguna (pembeli atau mitra) dan menghasilkan response dalam format JSON yang valid.
Format JSON yang diharapkan:
{{
  ""suggested_response"": ""draft balasan untuk pengguna"",
  ""intent"": ""salah satu dari: product_information, order_status, complaint, technical_support, human_request, unknown"",
  ""priority"": ""salah satu dari: Low, Normal, High, Urgent"",
  ""needs_human"": true/false (true jika pertanyaan kompleks, keluhan marah, atau minta agen manusia),
  ""reason"": ""alasan jika needs_human true""
}}

Gunakan informasi katalog/konteks berikut untuk menjawab pertanyaan:
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
            response.EnsureSuccessStatusCode();

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

            var dto = JsonSerializer.Deserialize<AiSuggestionDto>(answerStr);
            return dto ?? new AiSuggestionDto { SuggestedResponse = "Maaf, format balasan AI tidak sesuai." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Groq API");
            return new AiSuggestionDto 
            { 
                SuggestedResponse = "Maaf, terjadi kesalahan saat menghubungi layanan AI.",
                NeedsHuman = true,
                Priority = "High",
                Reason = "System Error"
            };
        }
    }
}
