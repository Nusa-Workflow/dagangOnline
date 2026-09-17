using System.Threading.Tasks;
using System.Windows.Input;
using dagangOnline.Mobile.Helpers;

namespace dagangOnline.Mobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        Title = "Masuk ke dagangOnline";
        LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
    }

    private Task ExecuteLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email dan kata sandi wajib diisi.";
            return Task.CompletedTask;
        }

        IsBusy = true;
        // Mock authentication flow for client UI
        ErrorMessage = string.Empty;
        IsBusy = false;
        return Task.CompletedTask;
    }
}
