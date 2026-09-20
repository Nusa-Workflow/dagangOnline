using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using dagangOnline.Application.DTOs;
using dagangOnline.Application.Interfaces;

namespace dagangOnline.Application.Services.Voice;

public class NemotronStrategicVoiceAgent : INemotronStrategicVoiceAgent
{
    private readonly ILongHorizonSyntheticDataEngine _syntheticEngine;
    private readonly INemotronVoiceAgentService _voiceService;
    private readonly ILogger<NemotronStrategicVoiceAgent> _logger;

    private static readonly string[] SupportedLanguages = new[]
    {
        "id",   // Bahasa Indonesia (Nasional)
        "jv",   // Basa Jawa (Jawa Tengah, Jatim, DIY)
        "su",   // Basa Sunda (Jawa Barat & Banten)
        "min",  // Baso Minangkabau (Sumatera Barat)
        "mad",  // Basa Madhura (Madura & Tapal Kuda)
        "ban",  // Basa Bali (Pulau Bali)
        "bug",  // Basa Bugis (Sulawesi Selatan)
        "bjn",  // Bahasa Banjar (Kalsel & Koridor IKN)
        "btk",  // Bahasa Batak (Sumatera Utara)
        "en"    // English (Global Enterprise)
    };

    public NemotronStrategicVoiceAgent(
        ILongHorizonSyntheticDataEngine syntheticEngine,
        INemotronVoiceAgentService voiceService,
        ILogger<NemotronStrategicVoiceAgent> logger)
    {
        _syntheticEngine = syntheticEngine;
        _voiceService = voiceService;
        _logger = logger;
    }

    public IReadOnlyList<string> GetSupportedLanguages() => SupportedLanguages;

    public async Task<LongHorizonForecastResultDto> GenerateStrategicForecastWithVoiceAsync(
        LongHorizonForecastRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Memulai prediksi strategis Nemotron {Years} tahun ke depan (2026 - {EndYear}) untuk sektor {Sector}, wilayah {Region}, bahasa {Lang}",
            request.HorizonYears, 2026 + request.HorizonYears, request.Sector, request.RegionalFocus, request.Language);

        // 1. Dapatkan proyeksi analitik & sintetik dari synthetic data engine
        var forecast = await _syntheticEngine.GenerateForecastAsync(request, cancellationToken);

        // 2. Susun naskah briefing vokal multilingual
        var language = string.IsNullOrWhiteSpace(request.Language) ? "id" : request.Language.ToLowerInvariant();
        forecast.Language = language;
        forecast.SpokenVoiceScript = GenerateSpokenBriefingScript(forecast, language);

        // 3. Sintesis audio menggunakan NVIDIA Nemotron Voice TTS jika diminta
        if (request.ReturnVoiceAudio)
        {
            try
            {
                var persona = language switch
                {
                    "su" => "Nemotron-Sunda-Prabu",
                    "jv" => "Nemotron-Jawa-Kusuma",
                    "min" => "Nemotron-Minang-Tuanku",
                    "mad" => "Nemotron-Madura-Trunojoyo",
                    "ban" => "Nemotron-Bali-Dewata",
                    "bug" => "Nemotron-Bugis-Sawerigading",
                    "bjn" => "Nemotron-Banjar-Pangeran",
                    "btk" => "Nemotron-Batak-Singamangaraja",
                    "en" => "Nemotron-Global-Executive",
                    _ => "Nemotron-Nusantara-Warm"
                };

                var targetLangTag = language switch
                {
                    "su" => "su-ID",
                    "jv" => "jv-ID",
                    "min" => "min-ID",
                    "mad" => "mad-ID",
                    "ban" => "ban-ID",
                    "bug" => "bug-ID",
                    "bjn" => "bjn-ID",
                    "btk" => "btk-ID",
                    "en" => "en-US",
                    _ => "id-ID"
                };

                var synthReq = new VoiceSynthesisRequestDto
                {
                    Text = forecast.SpokenVoiceScript,
                    TargetLanguage = targetLangTag,
                    VoicePersona = persona,
                    SpeechSpeed = 1.05,
                    Pitch = 1.0,
                    ReturnAudioStream = true
                };

                var synthResponse = await _voiceService.SynthesizeSpokenVoiceAsync(synthReq, cancellationToken);
                forecast.VoiceAudioBase64 = synthResponse.AudioBase64;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mensintesis audio suara Nemotron untuk naskah strategis, melanjutkan dengan teks.");
            }
        }

        sw.Stop();
        forecast.Telemetry.LatencyMs = sw.ElapsedMilliseconds;

        return forecast;
    }

    public string GenerateSpokenBriefingScript(LongHorizonForecastResultDto forecast, string language)
    {
        var start = forecast.StartYear;
        var end = forecast.EndYear;
        var sector = forecast.Sector;
        var region = forecast.RegionalFocus;
        var traj = forecast.Trajectory;
        var endPoint = traj.LastOrDefault();
        var endTech = endPoint?.TechAdoptionRate ?? 80.0;
        var endGreen = endPoint?.GreenLogisticsShare ?? 60.0;
        var endNeed = endPoint?.MarketNeedIndex ?? 85.0;

        return language switch
        {
            "su" => $"Sampurasun wargi sadaya. Ieu mangrupi pedaran strategi sora ti NVIDIA Nemotron. " +
                    $"Dina jangka lima dugi ka sapuluh taun kapayun, ti kawit taun {start} dugi ka taun {end}, " +
                    $"sektor {sector} di wewengkon {region} bakal ngalaman parobahan ageung. " +
                    $"Dina taun {end}, tingkat adopsi téknologi AI diperkirakeun dugi ka {endTech:F0} persén, " +
                    $"sareng distribusi héjo ngahontal {endGreen:F0} persén. " +
                    $"Kabutuhan utama konsumén nyaéta sistem logistik gancang anu terintegrasi sareng pamiartosan pinter. " +
                    $"Mugia ieu pedaran tiasa ngabantosan kamajengannana bisnis anjeun.",

            "jv" => $"Sugeng rawuh para mitra bisnis. Menika pawarta stratègis swanten saking NVIDIA Nemotron. " +
                    $"Wiwit taun {start} tumuju taun {end}, sektor {sector} ing wilayah {region} badhe ngalami owah-owahan ageng. " +
                    $"Ing taun {end}, kacekapan teknologi AI badhe nggayuh {endTech:F0} persen, " +
                    $"dene logistik ramah lingkungan dumugi {endGreen:F0} persen kanthi indeks kabutuhan pasar {endNeed:F0} saking satus. " +
                    $"Kabetahan ingkang paling baku inggih menika integrasi ranté pasok lan otomatisasi dagang. " +
                    $"Mugi-mugi pitedah menika saged dados margi kemajengan usaha panjenengan.",

            "min" => $"Salamaik datang dunsanak sadonyo. Iko laporan strategi suaro dari NVIDIA Nemotron. " +
                     $"Mambaliek limo sampai sapuluah taun ka muko, dari taun {start} sampai {end}, " +
                     $"sektor {sector} di ranah {region} ka maalami lonjakan gadang babasis teknologi cerdas. " +
                     $"Pado taun {end}, adopsi agen AI mandiri mancapai {endTech:F0} persen, " +
                     $"sarato logistik hijau marambah {endGreen:F0} persen jo indeks kabutuhan pasa {endNeed:F0} dari saratuih. " +
                     $"Kabutuhan utamo pambisnis adolah otomasi pangalehan, pergudangan capek, jo kas fintech real-time. " +
                     $"Mudah-mudahan wawasan ko manjadi jalan untuang galeh dunsanak kasadonyo.",

            "mad" => $"Salampet rabu taretan sadajana. Ka'dinto kabar strategi sowara dhari NVIDIA Nemotron. " +
                     $"Dhari taon {start} kantos {end}, sektor {sector} e daerah {region} bakal ngalamin kamajuwan rajâ. " +
                     $"E taon {end}, panyaluran teknologi AI bakal napa' {endTech:F0} persen, " +
                     $"bân logistik bhâghus ramah lingkungan napa' {endGreen:F0} persen kalabân indeks kabhutowan pasar {endNeed:F0} dhari saratos. " +
                     $"Kabhutowan sè palèng otama panèka otomatisasi dhâghâng bân rante pasok terpadu. " +
                     $"Mator sakalangkong, moghâ-moghâ bisnissa taretan sadaja sajan majhu bân barokah.",

            "ban" => $"Om Swastyastu semeton sami. Puniki orti strategi suara saking NVIDIA Nemotron. " +
                     $"Ngawit saking warsa {start} nyantos {end}, sektor {sector} ring wewidangan {region} " +
                     $"pacang ngamolihang pemargi ageng sane kalintang becik. " +
                     $"Ring warsa {end}, adopsi teknologi agen AI jagi nyujur {endTech:F0} persen, " +
                     $"sareng logistik asri ngantos {endGreen:F0} persen antuk indeks kabutuhan pasar {endNeed:F0} saking satus. " +
                     $"Sane pinih mabuat inggih punika integrasi dagang digital lan koridor distribusi anyar. " +
                     $"Matur suksma, dumogi rahayu lan ngawetuang kerahayuan ring usaha semeton.",

            "bug" => $"Salama' ki silessureng maneng. Iyanae ada passabbi strategi pau pole ri NVIDIA Nemotron. " +
                     $"Mappammula taung {start} lettu {end}, sektor {sector} ri wanua {region} " +
                     $"maelo tuwo massingkulu sibawa teknologi AI agentik. " +
                     $"Ri taung {end}, arajanna teknologi AI narapi {endTech:F0} persen, " +
                     $"nenniya laleng logistik mapaccing narapi {endGreen:F0} persen. " +
                     $"Parellunna dalle' mangolo iya ritu passompe' digital, pangngoloi dalle' madoro, na pammolina tauwe. " +
                     $"Kurru sumange', mamuare' maddupa dallena usahana silessureng maneng.",

            "bjn" => $"Salamat datang bubuhan pambisnis sabarataan. Ngini pandiran strategi suara matan NVIDIA Nemotron. " +
                     $"Matan tahun {start} sampai {end}, sektor {sector} di banua {region} " +
                     $"handak marasai parubahan ganal bapandukan lawan otomasi digital wan koridor IKN. " +
                     $"Pas tahun {end}, adopsi teknologi AI handak sampai {endTech:F0} parsen, " +
                     $"wan logistik hijau mancapai {endGreen:F0} parsen lawan indeks kabutuhan pasar {endNeed:F0} matan saratus. " +
                     $"Kabutuhan utama bubuhan kita adalah pergudangan lakas, voice agent pambalanjaran, wan modal kas digital. " +
                     $"Moga-moga wawasan ngini mambawa barakah wan bajaya gasan usaha bubuhan pian.",

            "btk" => $"Horas ma di hita sasudena dongan bisnis. On ma barita strategi soara sian NVIDIA Nemotron. " +
                     $"Mamungka taon {start} sahat tu {end}, sektor {sector} di luat {region} " +
                     $"lam tu majuna marhite teknologi AI dohot rantai pasok modern. " +
                     $"Di taon {end}, partumbuhan adopsi teknologi AI nunga sahat tu {endTech:F0} persen, " +
                     $"dohot logistik na ganjang sahat tu {endGreen:F0} persen marhite indeks pardagangan {endNeed:F0} sian saratus. " +
                     $"Na ringkot parjolo ima otomatisasi partigatigaan, pasar online, dohot modal na hatop. " +
                     $"Mauliate godang, sai horas jala dapot parsaulian ma di sude ulaon muna.",

            "en" => $"Greetings. This is your NVIDIA Nemotron Strategic Voice Briefing. " +
                    $"Projecting from {start} to {end}, a {forecast.HorizonYears}-year horizon, the {sector} sector in {region} " +
                    $"will undergo profound structural acceleration driven by agentic AI and decentralized green corridors. " +
                    $"By {end}, agentic automation adoption is projected to reach {endTech:F0}%, " +
                    $"green supply chain distribution will hit {endGreen:F0}%, and the market need index will stand at {endNeed:F0} out of 100. " +
                    $"Key enterprise imperatives include autonomous customer replenishments, sovereign local models, and seamless IKN logistics integration.",

            _ => $"Halo rekan bisnis Indonesia. Ini adalah ringkasan suara strategis langsung dari NVIDIA Nemotron. " +
                 $"Memetakan masa depan 5 hingga 10 tahun ke depan, dimulai dari tahun {start} hingga {end}, " +
                 $"sektor {sector} di kawasan {region} diproyeksikan bertransformasi secara akseleratif. " +
                 $"Pada tahun {end}, tingkat adopsi agen AI mandiri akan mencapai {endTech:F0} persen, " +
                 $"dengan pemanfaatan logistik hijau ramah lingkungan mencapai {endGreen:F0} persen. " +
                 $"Kebutuhan mendesak masa depan mencakup agen suara belanja otonom, integrasi koridor logistik IKN Nusantara, " +
                 $"dan permodalan kas dinamis berbasis analitik real-time. Manfaatkan wawasan ini untuk mengunci keunggulan kompetitif bisnis Anda."
        };
    }
}
