using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.Knowledge;

public class IndonesiaContextLayer : IIndonesiaContextLayer
{
    private static readonly Dictionary<string, string> AdministrativeGlossary = new(StringComparer.OrdinalIgnoreCase)
    {
        { "RT", "Rukun Tetangga (Tingkat lingkungan terkecil dalam rukun warga di Indonesia)" },
        { "RW", "Rukun Warga (Lembaga kemasyarakatan tingkat dusun/lingkungan)" },
        { "Kelurahan", "Wilayah kerja lurah sebagai perangkat daerah kabupaten/kota" },
        { "Desa", "Kesatuan masyarakat hukum yang memiliki batas wilayah berpemerintahan otonom" },
        { "Kecamatan", "Wilayah administratif pembagian dari kabupaten atau kota" },
        { "Kabupaten", "Pembagian wilayah administratif di bawah provinsi" },
        { "Provinsi", "Tingkat pertama wilayah administratif di Indonesia" }
    };

    private static readonly Dictionary<string, string> CommerceGlossary = new(StringComparer.OrdinalIgnoreCase)
    {
        { "UMKM", "Usaha Mikro, Kecil, dan Menengah (Pelaku bisnis lokal / Mitra binaan)" },
        { "Warung", "Toko kelontong atau gerai ritel mikro milik masyarakat lokal" },
        { "Pasar", "Pusat perdagangan fisik tradisional maupun pasar rakyat" },
        { "Mitra", "Partner penjual / merchant resmi yang terdaftar di platform dagangOnline" },
        { "Ongkir", "Ongkos kirim (Biaya logistik pengiriman barang)" },
        { "COD", "Cash on Delivery / Bayar di Tempat saat pesanan sampai" },
        { "Resi", "Nomor bukti pengiriman dari kurir logistik" }
    };

    public Task<string> EnrichContextWithLocalKnowledgeAsync(string query, string detectedLanguage, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INDONESIA CONTEXT & LOCAL GLOSSARY ===");

        bool matchedAny = false;

        foreach (var (term, explanation) in AdministrativeGlossary)
        {
            if (Regex.IsMatch(query, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            {
                sb.AppendLine($"- [Administrasi] {term}: {explanation}");
                matchedAny = true;
            }
        }

        foreach (var (term, explanation) in CommerceGlossary)
        {
            if (Regex.IsMatch(query, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            {
                sb.AppendLine($"- [Perdagangan/UMKM] {term}: {explanation}");
                matchedAny = true;
            }
        }

        if (!matchedAny)
        {
            sb.AppendLine("- Sistem dagangOnline beroperasi dalam ekosistem perdagangan Indonesia (UMKM, logistik nasional, dan mata uang Rupiah/IDR).");
        }

        sb.AppendLine($"Bahasa Deteksi Pengguna: {detectedLanguage}. Respons harus mematuhi kaidah bahasa pengguna dengan santun dan ramah.");

        return Task.FromResult(sb.ToString());
    }

    public bool ContainsRegionalAdministrativeTerms(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        foreach (var term in AdministrativeGlossary.Keys)
        {
            if (Regex.IsMatch(query, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
