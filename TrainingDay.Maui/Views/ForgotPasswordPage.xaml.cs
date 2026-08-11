using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(IAuthService authService)
    {
        InitializeComponent();
        BindingContext = new ForgotPasswordPageViewModel(authService);
    }
}
