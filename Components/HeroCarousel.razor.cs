using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using dagangOnline.Models;

namespace dagangOnline.Components;

public partial class HeroCarousel : ComponentBase, IDisposable
{
    [Parameter]
    public int AutoplayIntervalMs { get; set; } = 6000;

    [Parameter]
    public bool EnableAutoplay { get; set; } = true;

    [Parameter]
    public string Variant { get; set; } = "default"; // OneSignal A/B test variant hook

    public int CurrentIndex { get; private set; } = 0;
    public bool IsPaused { get; private set; } = false;
    public bool IsUserHovered { get; private set; } = false;

    private Timer? _autoplayTimer;
    private double _touchStartX = 0;
    private double _touchStartY = 0;
    private bool _disposed = false;

    public IReadOnlyList<HeroSlide> Slides { get; private set; } = new List<HeroSlide>();

    protected override void OnInitialized()
    {
        InitializeSlides();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && EnableAutoplay)
        {
            StartAutoplayTimer();
        }
    }

    private void InitializeSlides()
    {
        Slides = new List<HeroSlide>
        {
            new HeroSlide
            {
                Id = "umkm-growth",
                Index = 0,
                Eyebrow = "🤝 Sahabat Usaha Lokal",
                Title = "Solusi Jualan Online Praktis untuk UMKM Indonesia",
                Description = "Buka toko langsung aktif, kelola produk tanpa ribet, dan jangkau jutaan pembeli di seluruh Nusantara dengan biaya terjangkau dan ekosistem terpercaya.",
                PrimaryAction = new HeroAction
                {
                    Text = "Buka Toko Gratis",
                    Href = "/Account/Register",
                    AriaLabel = "Mulai pendaftaran toko UMKM gratis"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Jelajahi Produk",
                    Href = "/Products",
                    AriaLabel = "Lihat katalog produk UMKM lokal"
                },
                SystemStatus = "Gratis Daftar",
                VisualType = HeroVisualType.UmkmOperations
            },
            new HeroSlide
            {
                Id = "nationwide-delivery",
                Index = 1,
                Eyebrow = "🚚 Diskon Ongkir & Ekspedisi Nusantara",
                Title = "Jangkauan Pengiriman Cepat ke Seluruh Nusantara",
                Description = "Nikmati kemudahan kirim produk kerajinan, pangan, dan komoditas lokal langsung ke pelanggan di berbagai kota dengan potongan ongkir hingga 50% dan pelacakan resi real-time.",
                PrimaryAction = new HeroAction
                {
                    Text = "Cek Ekspedisi & Ongkir",
                    Href = "/Partnership",
                    AriaLabel = "Pelajari kemitraan ekspedisi dan pengiriman"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Lacak Kiriman Paket",
                    Href = "javascript:document.querySelector('.cs-bot-circle-btn')?.click();",
                    AriaLabel = "Lacak status kiriman paket Anda"
                },
                SystemStatus = "Kirim Tiap Hari",
                VisualType = HeroVisualType.MarketIntelligence
            },
            new HeroSlide
            {
                Id = "safe-escrow",
                Index = 2,
                Eyebrow = "💳 Rekening Bersama Bebas Cemas",
                Title = "Transaksi Aman & Pasti dengan Rekening Escrow",
                Description = "Jual beli tenang tanpa takut ditipu. Pembeli transfer dengan aman, penjual mendapat kepastian pembayaran dan pencairan dana langsung ke rekening bank lokal dalam 1x24 jam kerja.",
                PrimaryAction = new HeroAction
                {
                    Text = "Daftar Akun Mitra Gratis",
                    Href = "/Account/Register",
                    AriaLabel = "Mulai pendaftaran akun toko gratis"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Pelajari Sistem Escrow",
                    Href = "/Services",
                    AriaLabel = "Pelajari cara kerja rekening bersama terpercaya"
                },
                SystemStatus = "100% Terlindungi",
                VisualType = HeroVisualType.SovereignAi
            },
            new HeroSlide
            {
                Id = "friendly-support",
                Index = 3,
                Eyebrow = "💬 Pendampingan Ramah 24 Jam",
                Title = "Didampingi Asisten AI Cerdas & Tim CS Siaga",
                Description = "Ada kendala jualan, ingin rekomendasi harga pasar, atau butuh bantuan pelacakan pesanan? Mbak Siti dari Customer Service dan Asisten AI siap membantu Anda dengan ramah setiap saat.",
                PrimaryAction = new HeroAction
                {
                    Text = "Ngobrol dengan CS & AI",
                    Href = "javascript:document.querySelector('.cs-bot-circle-btn')?.click();",
                    AriaLabel = "Buka obrolan Customer Service dan Asisten AI"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Mengenal Kami",
                    Href = "/About",
                    AriaLabel = "Pelajari cerita tentang dagangOnline"
                },
                SystemStatus = "Siap Melayani",
                VisualType = HeroVisualType.HumanAiSupport
            }
        };
    }

    public void NextSlide()
    {
        GoToSlide((CurrentIndex + 1) % Slides.Count);
    }

    public void PrevSlide()
    {
        GoToSlide((CurrentIndex - 1 + Slides.Count) % Slides.Count);
    }

    public void GoToSlide(int index)
    {
        if (index < 0 || index >= Slides.Count) return;
        CurrentIndex = index;
        ResetTimer();
        StateHasChanged();
    }

    public void TogglePlayPause()
    {
        IsPaused = !IsPaused;
        if (IsPaused)
        {
            StopAutoplayTimer();
        }
        else
        {
            StartAutoplayTimer();
        }
        StateHasChanged();
    }

    public void OnMouseEnter()
    {
        IsUserHovered = true;
        StopAutoplayTimer();
    }

    public void OnMouseLeave()
    {
        IsUserHovered = false;
        if (!IsPaused && EnableAutoplay)
        {
            StartAutoplayTimer();
        }
    }

    public void OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "ArrowLeft":
                PrevSlide();
                break;
            case "ArrowRight":
                NextSlide();
                break;
            case "Home":
                GoToSlide(0);
                break;
            case "End":
                GoToSlide(Slides.Count - 1);
                break;
            case " ":
            case "Spacebar":
                TogglePlayPause();
                break;
        }
    }

    public void OnTouchStart(TouchEventArgs e)
    {
        if (e.Touches.Length > 0)
        {
            _touchStartX = e.Touches[0].ClientX;
            _touchStartY = e.Touches[0].ClientY;
        }
    }

    public void OnTouchEnd(TouchEventArgs e)
    {
        if (e.ChangedTouches.Length > 0)
        {
            var deltaX = e.ChangedTouches[0].ClientX - _touchStartX;
            var deltaY = e.ChangedTouches[0].ClientY - _touchStartY;

            // Ensure horizontal swipe is dominant and above threshold (40px)
            if (Math.Abs(deltaX) > Math.Abs(deltaY) && Math.Abs(deltaX) > 40)
            {
                if (deltaX < 0)
                {
                    NextSlide();
                }
                else
                {
                    PrevSlide();
                }
            }
        }
    }

    private void StartAutoplayTimer()
    {
        StopAutoplayTimer();
        if (_disposed) return;

        _autoplayTimer = new Timer(_ =>
        {
            InvokeAsync(() =>
            {
                if (!IsPaused && !IsUserHovered && !_disposed)
                {
                    NextSlide();
                }
            });
        }, null, AutoplayIntervalMs, AutoplayIntervalMs);
    }

    private void StopAutoplayTimer()
    {
        _autoplayTimer?.Dispose();
        _autoplayTimer = null;
    }

    private void ResetTimer()
    {
        if (!IsPaused && !IsUserHovered && EnableAutoplay)
        {
            StartAutoplayTimer();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        StopAutoplayTimer();
    }
}
