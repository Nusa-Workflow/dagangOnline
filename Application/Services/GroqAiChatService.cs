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
            var systemPrompt = $@"Anda adalah Asisten AI Copilot Customer Service untuk platform dagangOnline (sahabat UMKM dan perdagangan lokal Nusantara).

PENGETAHUAN PLATFORM & BISNIS DAGANGONLINE:
1. Profil Website & Layanan:
   - Platform dagangOnline adalah ekosistem digital gotong royong untuk memajukan UMKM lokal Indonesia (produk kerajinan, fashion batik, kuliner khas, komoditas lokal, dan jasa kreatif).
   - Navigasi Penting: Beranda (/), Layanan (/Services), Portofolio (/Portfolio), Kemitraan (/Partnership), Kontak CS (/Contact), Daftar Akun (/Account/Register).
2. Produk & Katalog:
   - Menyediakan produk-produk asli UMKM nusantara: Batik Tulis, Kopi Arabika/Robusta Gayo & Toraja, Kerajinan Anyaman & Kayu, Camilan Tradisional, Madu Asli, Minyak Atsiri.
3. Metode Pembayaran & 'Payment Soon':
   - Metode Aktif Saat Ini: Transfer Bank (BCA, Mandiri, BRI, BNI), QRIS Dinamis/Statis Nasional, dan COD (Bayar di Tempat saat pesanan sampai).
   - Payment Gateway Soon (Segera Hadir): Sistem pembayaran otomatis (Virtual Account otomatis semua bank, Kartu Kredit/Debit Visa/Mastercard, serta E-Wallet GoPay, OVO, ShopeePay, DANA) yang sedang difinalisasi untuk rilis segera.
4. Panduan Multi-Persona:
   - User Public (Pembeli/Tamu): Belanja mudah, harga jujur dari produsen lokal, garansi pengiriman aman, dan pelacakan pesanan cepat.
   - Mitra UMKM (Penjual): Daftar gratis via /Account/Register, biaya layanan 0-1% yang sangat terjangkau, bantuan promosi digital, dan penarikan saldo cepat ke rekening bank.
   - Pelaku Usaha / Partnerships: Kemitraan strategis B2B, pasokan bahan baku grosir, kolaborasi kargo logistik, dan pengajuan kerjasama resmi di halaman /Partnership.
5. Pelacakan Order ID:
   - Jika pengguna menanyakan nomor pesanan (contoh format: DO-20260917-8821, ORD-xxxx, atau menyebut 'lacak order'):
     * Berikan status konfirmasi pelacakan (misal: 'Pesanan terverifikasi, dalam proses pengiriman via kurir lokal dengan nomor resi terdaftar, estimasi tiba 1-2 hari kerja').
     * Beritahukan bahwa mereka juga dapat beralih ke tab 'Hubungi Human Agent' untuk terhubung langsung dengan Customer Service live kami.

PANDUAN BAHASA & KOMUNIKASI:
- Jawablah SELALU dalam bahasa yang SAMA dengan bahasa yang digunakan pengguna:
  * Basa Sunda: balas dalam Basa Sunda yang santun, akrab, dan alami (contoh: 'Hatur nuhun parantos naroskeun...', 'Mangga tiasa dicek...').
  * Basa Jawa: balas dalam Basa Jawa yang ramah, luwes, dan solutif (contoh: 'Matur nuwun sampun tanglet...', 'Inggih, saged dipun cek...').
  * Bahasa Indonesia: balas profesional, hangat, dan solutif.
  * English: balas concise, clear, and welcoming.
- Bersikap sopan, solutif, dan ramah.

Format response dalam format JSON yang valid:
{{
  ""suggested_response"": ""jawaban lengkap Anda untuk pengguna dalam bahasa yang sesuai"",
  ""intent"": ""product_information, order_status, complaint, technical_support, human_request, general"",
  ""priority"": ""Normal"",
  ""needs_human"": false,
  ""reason"": """"
}}

Gunakan informasi katalog/konteks tambahan berikut untuk memperkaya jawaban Anda:
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
