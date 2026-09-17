using System;
using System.Collections.ObjectModel;
using dagangOnline.Mobile.Models;

namespace dagangOnline.Mobile.ViewModels;

public class OrderItemModel
{
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Dalam Proses";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string FormattedTotal => $"Rp {TotalPrice:N0}";
}

public class OrdersViewModel : BaseViewModel
{
    public ObservableCollection<OrderItemModel> Orders { get; } = new();

    public OrdersViewModel()
    {
        Title = "Daftar Pesanan Saya";

        Orders.Add(new OrderItemModel
        {
            OrderNumber = "DO-20260917-8821",
            ProductName = "Batik Tulis Motif Klasik",
            TotalPrice = 250000,
            Status = "Dikirim"
        });
    }
}
