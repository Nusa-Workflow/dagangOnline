namespace dagangOnline.Application.DTOs;

/// <summary>
/// Permintaan prediksi live real-time 5 - 10 tahun (2026 - 2036) berbasis Nemotron & Synthetic Data.
/// </summary>
public class LongHorizonForecastRequestDto
{
    /// <summary>
    /// Sektor ekonomi: "Retail", "Agri", "F&B", "Tech", "Logistics", "Energy"
    /// </summary>
    public string Sector { get; set; } = "Retail";

    /// <summary>
    /// Durasi prediksi (5 sampai 10 tahun) mulai dari baseline 2026 (2026 - 2031 atau 2026 - 2036).
    /// </summary>
    public int HorizonYears { get; set; } = 5;

    /// <summary>
    /// Skenario sintetik: "Baseline", "TechGreenAcceleration", "GeopoliticalShock", "DemographicDividendMax"
    /// </summary>
    public string ScenarioType { get; set; } = "Baseline";

    /// <summary>
    /// Bahasa briefing suara: "id" (Bahasa Indonesia), "su" (Basa Sunda), "jv" (Basa Jawa), "en" (English)
    /// </summary>
    public string Language { get; set; } = "id";

    /// <summary>
    /// Wilayah fokus Indonesia: "Nasional", "IKN_Kalimantan", "Jawa_Bali", "Sumatera", "Indonesia_Timur"
    /// </summary>
    public string RegionalFocus { get; set; } = "Nasional";

    /// <summary>
    /// Apakah ingin menghasilkan audio base64 (RIFF/WAV) langsung dari NVIDIA Nemotron TTS
    /// </summary>
    public bool ReturnVoiceAudio { get; set; } = true;
}

/// <summary>
/// Titik proyeksi tahunan sepanjang horizon 2026 - 2036.
/// </summary>
public class LongHorizonTrajectoryPointDto
{
    public int Year { get; set; }
    public double MarketNeedIndex { get; set; } // Indeks kebutuhan pasar 0 - 100
    public decimal ProjectedOmsetUmkmIndex { get; set; } // Nilai proyektif (miliar Rp atau indeks basis 100)
    public double TechAdoptionRate { get; set; } // % adopsi AI agent & otomasi
    public double GreenLogisticsShare { get; set; } // % logistik rendah emisi / IKN corridor
    public double ConfidenceLower { get; set; }
    public double ConfidenceUpper { get; set; }
    public Dictionary<string, double> SyntheticVector { get; set; } = new(); // [G, O, W, Q] & macro drivers
    public string MacroContextSummary { get; set; } = string.Empty;
}

/// <summary>
/// Hasil ramalan strategis 5 - 10 tahun dengan analisis kebutuhan masa depan & suara Nemotron.
/// </summary>
public class LongHorizonForecastResultDto
{
    public string Sector { get; set; } = string.Empty;
    public int HorizonYears { get; set; }
    public int StartYear { get; set; } = 2026;
    public int EndYear { get; set; }
    public string ScenarioType { get; set; } = string.Empty;
    public string RegionalFocus { get; set; } = string.Empty;
    public string Language { get; set; } = "id";

    /// <summary>
    /// Ringkasan eksekutif pemahaman kebutuhan dan dinamika pasar
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// Kebutuhan masa depan konsumen dan UMKM di Indonesia
    /// </summary>
    public List<string> StrategicNeedsIdentified { get; set; } = new();

    /// <summary>
    /// Garis waktu proyeksi tahunan 2026 - 2036
    /// </summary>
    public List<LongHorizonTrajectoryPointDto> Trajectory { get; set; } = new();

    /// <summary>
    /// Peluang transformasi pasar & rekomendasi langkah taktis
    /// </summary>
    public List<string> ActionableRecommendations { get; set; } = new();

    /// <summary>
    /// Naskah pidato/briefing suara Nemotron dalam bahasa terpilih (id, su, jv, en)
    /// </summary>
    public string SpokenVoiceScript { get; set; } = string.Empty;

    /// <summary>
    /// Base64 payload audio WAV (16kHz PCM) hasil sintesis Nemotron Voice
    /// </summary>
    public string? VoiceAudioBase64 { get; set; }

    /// <summary>
    /// Telemetri performa model Nemotron Voice & generative engine
    /// </summary>
    public NemotronLongHorizonTelemetry Telemetry { get; set; } = new();
}

public class NemotronLongHorizonTelemetry
{
    public double LatencyMs { get; set; }
    public string ModelVariant { get; set; } = "NVIDIA-NemotronLabs-VoiceChat-11B-Strategic";
    public double AcousticConfidence { get; set; } = 0.98;
    public int SyntheticIterations { get; set; } = 1000;
    public string GeneratorEngine { get; set; } = "MonteCarlo-StateVector-ODE-GOWQ";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
