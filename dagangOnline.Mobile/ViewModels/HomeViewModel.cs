using System.Collections.ObjectModel;
using dagangOnline.Mobile.Models;

namespace dagangOnline.Mobile.ViewModels;

public class HomeViewModel : BaseViewModel
{
    public ObservableCollection<string> FeaturedCategories { get; } = new();

    public HomeViewModel()
    {
        Title = "Beranda dagangOnline";
        FeaturedCategories.Add("Kategori Kuliner & UMKM");
        FeaturedCategories.Add("Fashion & Kerajinan Lokal");
        FeaturedCategories.Add("Produk Pertanian & Bahan Pokok");
        FeaturedCategories.Add("Jasa & Layanan Mitra");
    }
}
