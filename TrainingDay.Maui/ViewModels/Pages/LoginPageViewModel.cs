using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService authService;

    public LoginPageViewModel(IAuthService authService)
    {
        this.authService = authService;
    }

    private string email;
    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    private string password;
    public string Password
    {
        get => password;
        set => SetProperty(ref password, value);
    }

    private bool rememberMe = true;
    public bool RememberMe
    {
        get => rememberMe;
        set => SetProperty(ref rememberMe, value);
    }

    private string errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => errorMessage;
        set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ICommand LoginCommand => new AsyncRelayCommand(Login);

    public ICommand GoToRegisterCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync(nameof(RegisterPage)));

    public ICommand GoToForgotPasswordCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync(nameof(ForgotPasswordPage)));

    private async Task Login()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppResources.FillAllFieldsString;
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await authService.LoginAsync(Email.Trim(), Password, RememberMe);
            if (result.Success)
            {
                LoggingService.TrackEvent("Login Succeeded");
                Password = string.Empty;
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
            ErrorMessage = AppResources.GenericErrorString;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
