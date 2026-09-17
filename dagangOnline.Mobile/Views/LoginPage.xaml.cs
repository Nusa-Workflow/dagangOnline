using dagangOnline.Mobile.ViewModels;

namespace dagangOnline.Mobile.Views;

public partial class LoginPage
{
    public LoginPage()
    {
        BindingContext = new LoginViewModel();
    }
}
