using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public class ForgotPasswordPageViewModel : BaseViewModel
{
    private readonly IAuthService authService;

    public ForgotPasswordPageViewModel(IAuthService authService)
    {
        this.authService = authService;
    }

    private string email;
    public string Email
    {
        get => email;
        set => SetProperty(ref email, value);
    }

    private string statusMessage = string.Empty;
    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public ICommand SubmitCommand => new AsyncRelayCommand(Submit);

    private async Task Submit()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Email))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await authService.ForgotPasswordAsync(Email.Trim());
            LoggingService.TrackEvent("Forgot Password Submitted");
            StatusMessage = AppResources.ResetLinkSentString;
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
            StatusMessage = AppResources.GenericErrorString;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
