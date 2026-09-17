using dagangOnline.Mobile.ViewModels;

namespace dagangOnline.Mobile.Views;

public partial class OrdersPage
{
    public OrdersPage()
    {
        BindingContext = new OrdersViewModel();
    }
}
