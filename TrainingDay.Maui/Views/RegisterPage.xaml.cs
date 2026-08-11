using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterPageViewModel viewModel;

    public RegisterPage(IAuthService authService)
    {
        InitializeComponent();
        viewModel = new RegisterPageViewModel(authService);
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.ErrorMessage = string.Empty;
    }
}
