using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

/// <summary>
/// Layanan NVIDIA Nemotron Strategic Voice Agent untuk analisis kebutuhan dan prediksi live real-time 5 - 10 tahun (2026 - 2036).
/// Menggabungkan pemodelan data sintetik, konteks Indonesia (IKN, hilirisasi, demografi), dan output suara multilingual (id, su, jv, en).
/// </summary>
public interface INemotronStrategicVoiceAgent
{
    /// <summary>
    /// Menghasilkan analisis kebutuhan strategis lengkap 5 - 10 tahun ke depan beserta naskah vokal dan sintesis audio Nemotron.
    /// </summary>
    Task<LongHorizonForecastResultDto> GenerateStrategicForecastWithVoiceAsync(
        LongHorizonForecastRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Menghasilkan naskah briefing vokal natural adaptif sesuai bahasa pilihan (id, su, jv, en).
    /// </summary>
    string GenerateSpokenBriefingScript(LongHorizonForecastResultDto forecast, string language);

    /// <summary>
    /// Daftar bahasa yang didukung untuk briefing suara strategis.
    /// </summary>
    IReadOnlyList<string> GetSupportedLanguages();
}
