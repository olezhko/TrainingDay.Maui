using Microsoft.AppCenter.Crashes;
using System.Windows.Input;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels;

internal class AuthViewModel : BaseViewModel
{
    private bool _isLogin;
    private bool _isRegisterAction;
    private bool _isLoginAction;
    private bool _isForgotPasswordAction;
    private string _email;
    private string _nick;
    private string _password;
    private ICommand _loginCommand;
    private ICommand _logoutCommand;
    private ICommand _forgotPasswordCommand;
    private ICommand _registerCommand;
    private ICommand _startActionCommand;
    private AuthActions _currentAction;

    public AuthViewModel()
    {
        IsLogin = Settings.Email.IsNotNullOrEmpty();
        if (IsLogin)
        {
            CurrentAction = AuthActions.Off;
        }


        GoogleEmail = Settings.Email;

        try
        {
            LastSyncDateTime = Settings.LastDatabaseSyncDateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        IsGoogleLogin = !string.IsNullOrWhiteSpace(GoogleEmail);
        GoogleCommand = new Command(async () => await OnAuthenticate("Google"));
        AppleCommand = new Command(async () => await OnAuthenticate("Apple"));
    }

    private async void Login()
    {
        if (Email.IsNullOrEmptyOrWhiteSpace() || Password.IsNullOrEmptyOrWhiteSpace())
        {
            //DependencyService.Get<IMessage>().ShortAlert("Filled not all fields");
            return;
        }

        //IsLogin = await SiteService.AuthLogin(Email, Password);
        if (!IsLogin)
            return;

        Settings.Email = Email;
        Settings.Password = Password;
        Settings.Nick = Nick;

        CurrentAction = AuthActions.Off;
    }

    private async void Register()
    {
        if (Email.IsNullOrEmptyOrWhiteSpace() || Password.IsNullOrEmptyOrWhiteSpace() || Nick.IsNullOrEmptyOrWhiteSpace())
        {
            //DependencyService.Get<IMessage>().ShortAlert("Filled not all fields");
            return;
        }

        //var result = await SiteService.AuthRegister(Email, Password, Nick);
        CurrentAction = AuthActions.Login;
    }

    private async void Forgot()
    {
        //var result = await SiteService.AuthForgotPassword(Email);
        //IsForgotPasswordAction = false;
        //IsLoginAction = result;
        //if (result)
        //{
        //    //DependencyService.Get<IMessage>().ShortAlert("To your Email sent generated password. Enter this value into password field.");
        //}
        //else
        //{
        //    //DependencyService.Get<IMessage>().ShortAlert("Email not found. You can register new account.");
        //    IsRegisterAction = true;
        //}
    }

    private void StartAction(AuthActions requiredAction)
    {
        switch (requiredAction)
        {
            case AuthActions.Login:
                IsLoginAction = true;
                IsRegisterAction = false;
                break;
            case AuthActions.Register:
                IsLoginAction = false;
                IsRegisterAction = true;
                break;
            case AuthActions.ForgotPassword:
                IsLoginAction = false;
                IsRegisterAction = false;
                IsForgotPasswordAction = true;
                break;
        }
        CurrentAction = requiredAction;
    }

    private void Logout()
    {
        Settings.Email = string.Empty;
        Settings.Password = string.Empty;
        Settings.Nick = string.Empty;
        IsLogin = false;
    }

    public ICommand LoginCommand => _loginCommand ?? (_loginCommand = new Command(Login));
    public ICommand LogoutCommand => _logoutCommand ?? (_logoutCommand = new Command(Logout));

    public ICommand RegisterCommand => _registerCommand ?? (_registerCommand = new Command(Register));
    public ICommand ForgotPasswordCommand => _forgotPasswordCommand ?? (_forgotPasswordCommand = new Command(Forgot));
    public ICommand StartActionCommand => _startActionCommand ?? (_startActionCommand = new Command<AuthActions>(StartAction));

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
    public string Nick
    {
        get => _nick;
        set => SetProperty(ref _nick, value);
    }
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }
    public bool IsLogin
    {
        get => _isLogin;
        set => SetProperty(ref _isLogin, value);
    }

    public bool IsRegisterAction
    {
        get => _isRegisterAction;
        set => SetProperty(ref _isRegisterAction, value);
    }

    public bool IsLoginAction
    {
        get => _isLoginAction;
        set => SetProperty(ref _isLoginAction, value);
    }

    public bool IsForgotPasswordAction
    {
        get => _isForgotPasswordAction;
        set => SetProperty(ref _isForgotPasswordAction, value);
    }

    public AuthActions CurrentAction
    {
        get => _currentAction;
        set => SetProperty(ref _currentAction, value);
    }


    private static readonly string AuthenticationUrl = $"{Consts.Site}/mobileauth/";
    private string accessToken = string.Empty;

    public bool IsSyncActive { get; set; }

    public string LastSyncDateTime { get; set; }

    public bool IsGoogleLogin { get; set; }

    public string GoogleEmail { get; set; }

    public ICommand GoogleCommand { get; }

    public ICommand AppleCommand { get; }

    public Command SyncGoogleCommand => new Command(SyncGoogle);

    public Command SyncFromGoogleCommand => new Command(SyncFromGoogle);

    public string AuthToken
    {
        get => accessToken;
        set => SetProperty(ref accessToken, value);
    }

    private async Task OnAuthenticate(string scheme)
    {
        try
        {
            WebAuthenticatorResult webAuthenticatorResult;

            if (scheme.Equals("Apple") && DeviceInfo.Platform == DevicePlatform.iOS && DeviceInfo.Version.Major >= 13)
            {
                webAuthenticatorResult = await AppleSignInAuthenticator.AuthenticateAsync();
            }
            else
            {
                var authUrl = new Uri(AuthenticationUrl + scheme);
                var callbackUrl = new Uri("trainingday://");

                webAuthenticatorResult = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
            }

            AuthToken = webAuthenticatorResult?.AccessToken ?? webAuthenticatorResult?.IdToken;
            if (scheme == "Google")
            {
                Settings.GoogleToken = AuthToken;
                if (webAuthenticatorResult.Properties.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
                {
                    Settings.Email = Uri.UnescapeDataString(email);
                    //SiteService.Email = Settings.Email;
                    GoogleEmail = Settings.Email;
                    IsGoogleLogin = !string.IsNullOrWhiteSpace(GoogleEmail);
                    OnPropertyChanged(nameof(IsGoogleLogin));
                    OnPropertyChanged(nameof(GoogleEmail));
                    //await SiteService.TokenUser(new MobileUserToken()
                    //{
                    //    //sUserId = Settings.,
                    //    Token = Settings.Token,
                    //});
                }
                else
                {
                    IsGoogleLogin = false;
                    OnPropertyChanged(nameof(IsGoogleLogin));
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            AuthToken = string.Empty;
            Console.WriteLine(ex.StackTrace);
        }
        catch (Exception ex)
        {
            AuthToken = string.Empty;
            Crashes.TrackError(ex);
            Console.WriteLine(ex.StackTrace);
        }
    }

    private async void SyncGoogle()
    {
        IsSyncActive = true;
        OnPropertyChanged(nameof(IsSyncActive));

        //await RepoSyncManager.UpdateRepo();
        LastSyncDateTime = Settings.LastDatabaseSyncDateTime;
        OnPropertyChanged(nameof(LastSyncDateTime));
        IsSyncActive = false;
        OnPropertyChanged(nameof(IsSyncActive));
        //DependencyService.Get<IMessage>().ShowNotification(PushMessagesManager.SyncFinishedId, AppResources.SynchronizationFinishedSuccessful, AppResources.Synchronization, false, false, false, null);
    }

    private async void SyncFromGoogle()
    {
        var result = await Shell.Current.DisplayAlert(AppResources.Synchronization, AppResources.SynchronizationDownloadingQuestion, AppResources.OkString, AppResources.CancelString);
        if (result)
        {
            IsSyncActive = true;
            OnPropertyChanged(nameof(IsSyncActive));

            //await RepoSyncManager.GetRepo();

            IsSyncActive = false;
            OnPropertyChanged(nameof(IsSyncActive));
            //DependencyService.Get<IMessage>().ShowNotification(PushMessagesManager.SyncFinishedId, AppResources.SynchronizationFinishedSuccessful, AppResources.Synchronization, false, false, false, null);
        }
    }
}

public enum AuthActions
{
    None = 0,
    Login,
    Register,
    ForgotPassword,
    Off
}