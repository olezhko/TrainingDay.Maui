using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel viewModel;

    public LoginPage(IAuthService authService)
    {
        InitializeComponent();
        viewModel = new LoginPageViewModel(authService);
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.ErrorMessage = string.Empty;
    }
}
