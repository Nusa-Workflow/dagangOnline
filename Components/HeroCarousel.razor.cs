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
    public int AutoplayIntervalMs { get; set; } = 7500;

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
                Id = "sovereign-ai",
                Index = 0,
                Eyebrow = "SOVEREIGN AI ENGINE",
                Title = "Sovereign AI Systems\nfor Every Decision",
                Description = "Infrastruktur kecerdasan artifisial terdesentralisasi untuk membantu rantai pasok, intelijen harga pasar, dan pertumbuhan mandiri bagi UMKM Indonesia.",
                PrimaryAction = new HeroAction
                {
                    Text = "Jelajahi Ekosistem AI",
                    Href = "/Services",
                    AriaLabel = "Pelajari infrastruktur kecerdasan artifisial dagangOnline"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Konsultasi Sistem",
                    Href = "/Contact",
                    AriaLabel = "Hubungi tim spesialis untuk konsultasi teknologi"
                },
                VisualType = HeroVisualType.SovereignAi,
                SystemStatus = "OPERATIONAL",
                Metadata = new List<HeroMetadataItem>
                {
                    new() { Label = "SYSTEM", Value = "OPERATIONAL", Status = "active" },
                    new() { Label = "NETWORK", Value = "ACTIVE", Status = "live" },
                    new() { Label = "AI LAYER", Value = "ONLINE", Status = "active" }
                }
            },
            new HeroSlide
            {
                Id = "market-intelligence",
                Index = 1,
                Eyebrow = "MARKET INTELLIGENCE",
                Title = "Turn Local Signals\ninto Market Intelligence",
                Description = "Gabungkan sinyal transaksi, permintaan, harga, dan aktivitas pasar menjadi insight yang dapat digunakan UMKM secara real-time.",
                PrimaryAction = new HeroAction
                {
                    Text = "Pantau Sinyal Pasar",
                    Href = "/Portfolio",
                    AriaLabel = "Buka data pergerakan komoditas dan aktivitas pasar"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Katalog Komoditas",
                    Href = "/Services",
                    AriaLabel = "Lihat katalog pasokan produk UMKM nusantara"
                },
                VisualType = HeroVisualType.MarketIntelligence,
                SystemStatus = "LIVE STREAM",
                Metadata = new List<HeroMetadataItem>
                {
                    new() { Label = "MARKET", Value = "LIVE", Status = "live" },
                    new() { Label = "SIGNALS", Value = "ACTIVE", Status = "active" },
                    new() { Label = "DATA STREAM", Value = "ONLINE", Status = "active" }
                }
            },
            new HeroSlide
            {
                Id = "umkm-operations",
                Index = 2,
                Eyebrow = "UMKM OPERATIONS",
                Title = "One Operational Layer\nfor Every UMKM",
                Description = "Satukan operasional, inventory, customer interaction, partnership, dan intelligence dalam satu platform berdaulat.",
                PrimaryAction = new HeroAction
                {
                    Text = "Buka Panel Operasi",
                    Href = "/Account/Register",
                    AriaLabel = "Mulai pendaftaran akun toko atau mitra dagangOnline"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Daftar Mitra UMKM",
                    Href = "/Partnership",
                    AriaLabel = "Pelajari program kerjasama pelaku usaha dan mitra"
                },
                VisualType = HeroVisualType.UmkmOperations,
                SystemStatus = "SYNCED",
                Metadata = new List<HeroMetadataItem>
                {
                    new() { Label = "ORDERS", Value = "ACTIVE", Status = "live" },
                    new() { Label = "INVENTORY", Value = "SYNCED", Status = "active" },
                    new() { Label = "PARTNERS", Value = "ONLINE", Status = "active" }
                }
            },
            new HeroSlide
            {
                Id = "human-ai",
                Index = 3,
                Eyebrow = "AI + HUMAN SUPPORT",
                Title = "AI When You Need It.\nHumans When It Matters.",
                Description = "Pisahkan AI assistance dan human support dalam workflow yang jelas, aman, dan mudah digunakan untuk pendampingan wirausaha.",
                PrimaryAction = new HeroAction
                {
                    Text = "Buka CS Live & AI",
                    Href = "javascript:document.querySelector('.cs-bot-circle-btn')?.click();",
                    AriaLabel = "Buka drawer obrolan Asisten AI dan CS Human langsung"
                },
                SecondaryAction = new HeroAction
                {
                    Text = "Alur Pendampingan",
                    Href = "/About",
                    AriaLabel = "Pelajari nilai pendampingan ramah dagangOnline"
                },
                VisualType = HeroVisualType.HumanAiSupport,
                SystemStatus = "READY",
                Metadata = new List<HeroMetadataItem>
                {
                    new() { Label = "AI", Value = "ONLINE", Status = "active" },
                    new() { Label = "HUMAN AGENT", Value = "AVAILABLE", Status = "live" },
                    new() { Label = "ROUTING", Value = "ACTIVE", Status = "active" }
                }
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
