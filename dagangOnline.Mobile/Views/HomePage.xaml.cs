using dagangOnline.Mobile.ViewModels;

namespace dagangOnline.Mobile.Views;

public partial class HomePage
{
    public HomePage()
    {
        BindingContext = new HomeViewModel();
    }
}
