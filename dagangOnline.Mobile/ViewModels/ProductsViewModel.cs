using System.Collections.ObjectModel;
using dagangOnline.Mobile.Models;

namespace dagangOnline.Mobile.ViewModels;

public class ProductsViewModel : BaseViewModel
{
    public ObservableCollection<ProductModel> Products { get; } = new();

    public ProductsViewModel()
    {
        Title = "Katalog Produk & UMKM";

        Products.Add(new ProductModel { Name = "Kopi Robusta Lereng Gunung", Summary = "Kopi sangrai UMKM lokal khas pegunungan", Price = 45000, Category = "Kuliner" });
        Products.Add(new ProductModel { Name = "Batik Tulis Motif Klasik", Summary = "Kain batik katun premium kerajinan tangan", Price = 250000, Category = "Fashion" });
        Products.Add(new ProductModel { Name = "Madu Murni Hutan Asli", Summary = "Madu alami 500ml tanpa bahan pengawet", Price = 85000, Category = "Kesehatan" });
    }
}
