using System.Threading.Tasks;
using dagangOnline.Domain.Chat;

namespace dagangOnline.Services;

public class ChatBotService
{
    public async Task<string> GetReplyAsync(ChatSession session, string message)
    {
        // Simple mock bot logic
        var lowerMessage = message.ToLowerInvariant();
        
        // Simulating some processing time
        await Task.Delay(500);

        if (lowerMessage.Contains("bantuan") || lowerMessage.Contains("agen") || lowerMessage.Contains("manusia") || lowerMessage.Contains("cs"))
        {
            return "Sepertinya Anda membutuhkan bantuan lebih lanjut. Silakan klik tombol 'Eskalasi ke Agen' atau saya bisa meneruskan Anda ke Customer Service kami jika Anda memintanya dengan jelas.";
        }
        else if (lowerMessage.Contains("halo") || lowerMessage.Contains("hai"))
        {
            return "Halo! Saya adalah asisten virtual DagangOnline. Ada yang bisa saya bantu hari ini?";
        }
        else if (lowerMessage.Contains("harga") || lowerMessage.Contains("biaya"))
        {
            return "Untuk informasi harga, silakan cek halaman layanan atau katalog publik kami. Apakah ada spesifikasi khusus yang Anda cari?";
        }
        else
        {
            return "Maaf, saya masih belajar dan mungkin belum memahami pertanyaan Anda sepenuhnya. Anda dapat mengetik 'bantuan' untuk opsi lebih lanjut.";
        }
    }
}
