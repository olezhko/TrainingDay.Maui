using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public class RegisterPageViewModel : BaseViewModel
{
    private readonly IAuthService authService;

    public RegisterPageViewModel(IAuthService authService)
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

    private string nickname;
    public string Nickname
    {
        get => nickname;
        set => SetProperty(ref nickname, value);
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

    public ICommand RegisterCommand => new AsyncRelayCommand(Register);

    private async Task Register()
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
            var nick = string.IsNullOrWhiteSpace(Nickname) ? null : Nickname.Trim();
            var result = await authService.RegisterAsync(Email.Trim(), Password, nick);
            if (result.Success)
            {
                LoggingService.TrackEvent("Register Succeeded");
                await Toast.Make(AppResources.ConfirmEmailPromptString).Show();
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
