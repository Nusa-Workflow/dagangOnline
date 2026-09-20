using dagangOnline.Application.DTOs;

namespace dagangOnline.Application.Interfaces;

/// <summary>
/// Mesin pemodelan analitik data empiris dan data sintetik untuk proyeksi 5 - 10 tahun (2026 - 2036).
/// Mengintegrasikan vektor keadaan diferensial [G, O, W, Q], indikator makro Indonesia, IKN, hilirisasi komoditas, dan demografi.
/// </summary>
public interface ILongHorizonSyntheticDataEngine
{
    /// <summary>
    /// Menghasilkan lintasan proyeksi tahunan 2026 sampai 2031 (5 thn) atau 2036 (10 thn) berbasis skenario sintetik Monte Carlo.
    /// </summary>
    Task<LongHorizonForecastResultDto> GenerateForecastAsync(LongHorizonForecastRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Daftar sektor dan skenario yang didukung beserta konteks wilayah Indonesia.
    /// </summary>
    IReadOnlyList<string> GetSupportedSectors();
    IReadOnlyList<string> GetSupportedScenarios();
    IReadOnlyList<string> GetSupportedRegions();
}
