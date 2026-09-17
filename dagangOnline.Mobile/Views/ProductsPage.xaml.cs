using dagangOnline.Mobile.ViewModels;

namespace dagangOnline.Mobile.Views;

public partial class ProductsPage
{
    public ProductsPage()
    {
        BindingContext = new ProductsViewModel();
    }
}
